# Host Management & Deployment - API Specification
# For frontend AI agent to design the system calling API

> Base URL: `http://<server>:5156`
> Content-Type: `application/json`
> All responses wrapped in `BaseResponse<T>`

---

## Architecture Overview

```
Frontend
   |
   v
HostManagement.API (:5156)     <-- This spec. Central management.
   |                                Stores host IP/port + service config in DB.
   |                                Frontend calls THIS API for everything.
   |
   |-- [HTTP internally] -->  HelperTool.API (:5155 on each host)
                                   Reads DB for composeFilePath/workingDirectory.
                                   Executes `docker compose` locally.
                                   Manages local Redis cache directly.
                                   Frontend does NOT call this directly.
```

---

## Response Wrapper

Every endpoint returns:

```typescript
interface BaseResponse<T> {
  isSuccess: boolean;
  data?: T;
  message?: string;
}
```

---

## Enums (string values in JSON)

```typescript
type HostStatus = "Online" | "Offline" | "Degraded" | "Unknown";

type ServiceStatus = "Running" | "Stopped" | "Error" | "Unknown";

type ServiceType =
  | "DockerCompose"     // Docker Compose stack (.yml file)
  | "DockerContainer"   // Single container
  | "Systemd"           // Linux systemd service
  | "WindowsService"
  | "WebApp"
  | "Database"
  | "Other";

type DeploymentStatus = "Pending" | "InProgress" | "Success" | "Failed" | "RolledBack";
```

---

## Data Model Relationships

```
MonitoredHost (1) --->> (N) MonitoredService (1) --->> (N) DeploymentHistory
     |                          |
     | ipAddress                | composeFilePath
     | agentPort                | workingDirectory
     | sshPort                  | deployCommand
     | sshUsername              | containerName
     | status                   | status
     | isActive                 | lastDeploymentStatus
     |                          | lastDeployedAt
```

---

# ENDPOINTS

---

## 1. DASHBOARD

### GET /api/hosts/dashboard

Returns aggregated counts of hosts/services by status, plus host list.

**Response:** `BaseResponse<DashboardDto>`

```typescript
interface DashboardDto {
  totalHosts: number;          // Only active hosts counted
  onlineHosts: number;
  offlineHosts: number;
  totalServices: number;       // All services (all hosts)
  runningServices: number;
  stoppedServices: number;
  errorServices: number;
  hosts: HostDto[];            // Active hosts with service counts
}
```

**Example Response:**
```json
{
  "isSuccess": true,
  "data": {
    "totalHosts": 2,
    "onlineHosts": 1,
    "offlineHosts": 1,
    "totalServices": 12,
    "runningServices": 10,
    "stoppedServices": 1,
    "errorServices": 1,
    "hosts": [
      {
        "id": "a1b2c3d4-...",
        "name": "UAT Server 192.168.100.13",
        "ipAddress": "192.168.100.13",
        "agentPort": 5155,
        "status": "Online",
        "isActive": true,
        "serviceCount": 10,
        "runningServiceCount": 8
      }
    ]
  },
  "message": "Dashboard"
}
```

---

## 2. HOST CRUD

### HostDto (returned for list views)

```typescript
interface HostDto {
  id: string;                    // UUID
  name: string;
  description?: string;
  ipAddress: string;             // e.g. "192.168.100.13"
  agentPort: number;             // HelperTool.API port on this host (default 5155)
  sshPort?: number;              // SSH port (default 22)
  sshUsername?: string;          // SSH user
  os?: string;                   // e.g. "Ubuntu"
  status: HostStatus;            // "Online" | "Offline" | "Degraded" | "Unknown"
  lastCheckedAt?: string;        // ISO 8601 - last health check timestamp
  isActive: boolean;
  serviceCount: number;          // total services on this host
  runningServiceCount: number;   // services with status "Running"
  createdAt: string;             // ISO 8601
  updatedAt: string;             // ISO 8601
}
```

### HostDetailDto (returned for single host views)

```typescript
interface HostDetailDto extends HostDto {
  services: ServiceDto[];        // all services belonging to this host
}
```

---

### GET /api/hosts

List all hosts. Optionally filter by active status.

**Query Parameters:**

| Param      | Type    | Required | Default | Description                 |
|------------|---------|----------|---------|-----------------------------|
| activeOnly | boolean | No       | null    | true = only active hosts    |

**Response:** `BaseResponse<HostDto[]>`

**HTTP Status:** Always 200

---

### GET /api/hosts/{hostId}

Get single host with all its services.

**Path Parameters:**

| Param  | Type | Description |
|--------|------|-------------|
| hostId | UUID | Host ID     |

**Response:** `BaseResponse<HostDetailDto>`

**HTTP Status:** 200 OK | 404 Not Found

---

### POST /api/hosts

Create a new host.

**Request Body:**

```typescript
interface CreateHostRequest {
  name: string;                  // required - display name
  description?: string;
  ipAddress: string;             // required - must be unique across all hosts
  agentPort?: number;            // default: 5155
  sshPort?: number;              // default: 22
  sshUsername?: string;
  sshPrivateKeyPath?: string;
  sshPassword?: string;
  os?: string;
}
```

**Response:** `BaseResponse<HostDetailDto>`

**HTTP Status:** 201 Created | 400 Bad Request (duplicate IP)

**Business Rules:**
- `ipAddress` must be unique. If duplicate: `{ isSuccess: false, message: "Host with IP '...' already exists" }`
- New host starts with `status: "Unknown"`

---

### PUT /api/hosts/{hostId}

Partial update - only send fields you want to change.

**Request Body:**

```typescript
interface UpdateHostRequest {
  name?: string;
  description?: string;
  ipAddress?: string;            // must be unique if changed
  agentPort?: number;
  sshPort?: number;
  sshUsername?: string;
  sshPrivateKeyPath?: string;
  sshPassword?: string;
  os?: string;
  isActive?: boolean;            // can deactivate/reactivate host
}
```

**Response:** `BaseResponse<HostDetailDto>`

**HTTP Status:** 200 OK | 404 Not Found | 400 Bad Request (duplicate IP)

---

### DELETE /api/hosts/{hostId}

Delete host. **Cascade deletes all services and deployment histories.**

**Response:** `BaseResponse<object>`

**HTTP Status:** 200 OK | 404 Not Found

---

## 3. SERVICE CRUD (nested under host)

### ServiceDto

```typescript
interface ServiceDto {
  id: string;                     // UUID
  hostId: string;                 // UUID - parent host
  hostName: string;               // parent host display name

  // Identity
  name: string;                   // service name (unique per host)
  description?: string;
  type: ServiceType;              // "DockerCompose" | "DockerContainer" | ...
  status: ServiceStatus;          // "Running" | "Stopped" | "Error" | "Unknown"

  // Network
  port?: number;                  // exposed port
  healthCheckUrl?: string;        // health check endpoint

  // Docker config
  imageName?: string;             // docker image name
  version?: string;               // current version/tag

  // Deployment config (stored in DB, used by agent)
  composeFilePath?: string;       // absolute path to .yml on the host
  workingDirectory?: string;      // working directory on the host
  dockerfilePath?: string;
  containerName?: string;

  // Custom commands (override default docker compose behavior)
  deployCommand?: string;         // if set, agent runs this instead of compose
  stopCommand?: string;
  restartCommand?: string;

  // Deployment status
  lastDeploymentStatus?: DeploymentStatus;  // last deploy result
  lastDeployedAt?: string;                  // ISO 8601

  // Monitoring
  lastCheckedAt?: string;         // ISO 8601 - last monitor check
  startedAt?: string;             // ISO 8601 - when service started

  // Meta
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}
```

---

### GET /api/hosts/{hostId}/services

Get all services for a host.

**Response:** `BaseResponse<ServiceDto[]>`

**HTTP Status:** 200 OK | 404 Not Found (host not found)

---

### GET /api/hosts/{hostId}/services/{serviceId}

Get single service detail.

**Response:** `BaseResponse<ServiceDto>`

**HTTP Status:** 200 OK | 404 Not Found

---

### POST /api/hosts/{hostId}/services

Add a service to a host.

**Request Body:**

```typescript
interface CreateServiceRequest {
  name: string;                   // required - unique within this host
  description?: string;
  type?: ServiceType;             // default: "DockerContainer"
  port?: number;
  healthCheckUrl?: string;
  imageName?: string;
  version?: string;
  composeFilePath?: string;       // e.g. "/root/tadmin/tadmin_api.yml"
  workingDirectory?: string;      // e.g. "/root/tadmin"
  dockerfilePath?: string;
  containerName?: string;
  deployCommand?: string;         // custom deploy command (overrides compose)
  stopCommand?: string;
  restartCommand?: string;
}
```

**Response:** `BaseResponse<ServiceDto>`

**HTTP Status:** 201 Created | 400 Bad Request

**Business Rules:**
- `name` must be unique within the host
- New service starts with `status: "Unknown"`

**Deploy Command Resolution (on agent side):**
1. If `deployCommand` is set -> use it directly
2. Else if `composeFilePath` is set -> `docker compose -f {file} down --rmi all && docker compose -f {file} up -d`
3. Else if `containerName` + `imageName` -> `docker stop/rm/pull/run`
4. Else -> error "No deploy command configured"

---

### PUT /api/hosts/{hostId}/services/{serviceId}

Partial update - only send fields you want to change.

**Request Body:**

```typescript
interface UpdateServiceRequest {
  name?: string;
  description?: string;
  type?: ServiceType;
  port?: number;
  healthCheckUrl?: string;
  imageName?: string;
  version?: string;
  composeFilePath?: string;
  workingDirectory?: string;
  dockerfilePath?: string;
  containerName?: string;
  deployCommand?: string;
  stopCommand?: string;
  restartCommand?: string;
  isActive?: boolean;
}
```

**Response:** `BaseResponse<ServiceDto>`

**HTTP Status:** 200 OK | 404 Not Found

---

### DELETE /api/hosts/{hostId}/services/{serviceId}

Delete a service. **Cascade deletes deployment histories.**

**Response:** `BaseResponse<object>`

**HTTP Status:** 200 OK | 404 Not Found

---

## 4. DEPLOYMENT

### POST /api/deployments

**Deploy a service by name.** This is the main action endpoint.

The system:
1. Looks up the service by name in DB
2. Finds the parent host (IP + agentPort)
3. Calls `POST http://{host.ip}:{host.agentPort}/api/deploy` on the remote agent
4. Agent reads `composeFilePath`/`workingDirectory` from DB
5. Agent executes `docker compose -f {file} down --rmi all && docker compose -f {file} up -d`
6. Result is saved to `deployment_histories` table
7. Service `lastDeploymentStatus` and `lastDeployedAt` are updated

**Request Body:**

```typescript
interface DeployByNameRequest {
  serviceName: string;           // required - exact match on service name in DB
  version?: string;              // optional - new version tag
  triggeredBy?: string;          // optional - who triggered (default: "API")
}
```

**Response:** `BaseResponse<DeploymentResultDto>`

```typescript
interface DeploymentResultDto {
  deploymentId: string;          // UUID - deployment history record ID
  serviceId: string;             // UUID
  serviceName: string;
  hostId: string;                // UUID - which host was deployed to
  hostName: string;
  hostIp: string;                // the IP address of the host
  status: DeploymentStatus;      // "Success" | "Failed" | "InProgress"
  output?: string;               // stdout from docker compose
  errorMessage?: string;         // stderr or exception message
  startedAt: string;             // ISO 8601
  finishedAt?: string;           // ISO 8601 - null if still in progress
  durationMs?: number;           // execution time in milliseconds
}
```

**HTTP Status:** 200 OK | 400 Bad Request

**Example - Deploy a service:**
```json
// Request
POST /api/deployments
{
  "serviceName": "tcis_ttos_tadmin_api_uat",
  "triggeredBy": "admin@tcis.vn"
}

// Response (success)
{
  "isSuccess": true,
  "data": {
    "deploymentId": "f47ac10b-...",
    "serviceId": "550e8400-...",
    "serviceName": "tcis_ttos_tadmin_api_uat",
    "hostId": "6ba7b810-...",
    "hostName": "UAT Server 192.168.100.13",
    "hostIp": "192.168.100.13",
    "status": "Success",
    "output": "Container tadmin-api-1 Started\n",
    "errorMessage": null,
    "startedAt": "2025-01-10T10:00:00Z",
    "finishedAt": "2025-01-10T10:00:12Z",
    "durationMs": 12345
  },
  "message": "Service 'tcis_ttos_tadmin_api_uat' deployed successfully on host 'UAT Server 192.168.100.13'"
}
```

**Example - Service not found:**
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Service 'nonexistent' not found or inactive"
}
```

**Example - Agent unreachable:**
```json
{
  "isSuccess": false,
  "data": {
    "deploymentId": "...",
    "status": "Failed",
    "errorMessage": "Cannot connect to HelperTool agent at 192.168.100.13:5155 — Connection refused",
    "durationMs": 5023
  },
  "message": "Cannot connect to HelperTool agent at 192.168.100.13:5155 — Connection refused"
}
```

**Error Scenarios:**

| Scenario | isSuccess | message |
|----------|-----------|---------|
| Service name not found / inactive | false | "Service '...' not found or inactive" |
| Host not found for service | false | "Host for service '...' not found" |
| Host is inactive | false | "Host '...' (ip) is inactive" |
| Agent unreachable | false | "Cannot connect to HelperTool agent at ip:port — ..." |
| Agent timeout (>10 min) | false | "Request to HelperTool agent timed out — ..." |
| Docker compose fails | false | "Deployment failed for service '...'" (with errorMessage in data) |

---

### GET /api/deployments/history/{serviceId}

Get deployment history for a service by its UUID.

**Path Parameters:**

| Param     | Type | Description |
|-----------|------|-------------|
| serviceId | UUID | Service ID  |

**Query Parameters:**

| Param | Type | Default | Description              |
|-------|------|---------|--------------------------|
| take  | int  | 20      | Max records to return    |

**Response:** `BaseResponse<DeploymentHistoryDto[]>`

```typescript
interface DeploymentHistoryDto {
  id: string;                    // UUID - deployment record ID
  serviceId: string;             // UUID
  serviceName: string;
  status: DeploymentStatus;      // "Success" | "Failed" | "InProgress" | ...
  version?: string;              // version deployed
  triggeredBy?: string;          // who triggered
  output?: string;               // stdout
  errorMessage?: string;         // stderr
  startedAt: string;             // ISO 8601
  finishedAt?: string;           // ISO 8601
  durationMs?: number;           // execution time ms
}
```

**HTTP Status:** 200 OK | 404 Not Found (service not found)

**Records are sorted by `startedAt` descending (newest first).**

---

### GET /api/deployments/history/by-name/{serviceName}

Same as above but lookup by service name instead of UUID.

**Path Parameters:**

| Param       | Type   | Description                |
|-------------|--------|----------------------------|
| serviceName | string | Exact service name in DB   |

**Query Parameters:**

| Param | Type | Default | Description              |
|-------|------|---------|--------------------------|
| take  | int  | 20      | Max records to return    |

**Response:** `BaseResponse<DeploymentHistoryDto[]>`

**HTTP Status:** 200 OK | 404 Not Found

---

# ENDPOINT SUMMARY TABLE

| #  | Method | Endpoint                                      | Description                         | Returns                          |
|----|--------|-----------------------------------------------|-------------------------------------|----------------------------------|
| 1  | GET    | /api/hosts/dashboard                          | System overview + stats             | BaseResponse<DashboardDto>       |
| 2  | GET    | /api/hosts?activeOnly=                        | List all hosts                      | BaseResponse<HostDto[]>          |
| 3  | GET    | /api/hosts/{hostId}                           | Host detail + services              | BaseResponse<HostDetailDto>      |
| 4  | POST   | /api/hosts                                    | Create host                         | BaseResponse<HostDetailDto>      |
| 5  | PUT    | /api/hosts/{hostId}                           | Update host (partial)               | BaseResponse<HostDetailDto>      |
| 6  | DELETE | /api/hosts/{hostId}                           | Delete host + cascade               | BaseResponse<object>             |
| 7  | GET    | /api/hosts/{hostId}/services                  | List services of host               | BaseResponse<ServiceDto[]>       |
| 8  | GET    | /api/hosts/{hostId}/services/{serviceId}      | Service detail                      | BaseResponse<ServiceDto>         |
| 9  | POST   | /api/hosts/{hostId}/services                  | Add service to host                 | BaseResponse<ServiceDto>         |
| 10 | PUT    | /api/hosts/{hostId}/services/{serviceId}      | Update service (partial)            | BaseResponse<ServiceDto>         |
| 11 | DELETE | /api/hosts/{hostId}/services/{serviceId}      | Delete service + cascade            | BaseResponse<object>             |
| 12 | POST   | /api/deployments                              | Deploy service by name              | BaseResponse<DeploymentResultDto>|
| 13 | GET    | /api/deployments/history/{serviceId}?take=    | Deploy history by service ID        | BaseResponse<DeploymentHistoryDto[]> |
| 14 | GET    | /api/deployments/history/by-name/{name}?take= | Deploy history by service name      | BaseResponse<DeploymentHistoryDto[]> |
| 15 | GET    | /api/redis/{hostName}/info                    | Redis server info (via agent)       | BaseResponse<RedisInfoDto>       |
| 16 | GET    | /api/redis/{hostName}/keys?pattern=&maxCount= | Search Redis keys (via agent)       | BaseResponse<RedisKeyListDto>    |
| 17 | GET    | /api/redis/{hostName}/keys/{key}              | Get Redis key value (via agent)     | BaseResponse<RedisKeyDto>        |
| 18 | POST   | /api/redis/{hostName}/keys                    | Set Redis key (via agent)           | BaseResponse<RedisKeyDto>        |
| 19 | DELETE | /api/redis/{hostName}/keys                    | Delete Redis keys by pattern        | BaseResponse<DeleteKeysResultDto>|
| 20 | DELETE | /api/redis/{hostName}/flush                   | Flush Redis database                | BaseResponse<FlushResultDto>     |

---

# TYPICAL FRONTEND FLOWS

## Flow 1: Dashboard Page
```
GET /api/hosts/dashboard
  -> Display totalHosts, onlineHosts, totalServices, runningServices
  -> Display host cards with serviceCount, runningServiceCount
  -> Click host card -> navigate to host detail
```

## Flow 2: Host Detail Page
```
GET /api/hosts/{hostId}
  -> Display host info (name, ip, agentPort, status)
  -> Display services table
  -> Each service row shows: name, type, status, lastDeploymentStatus, lastDeployedAt
  -> Actions per service: Deploy, Edit, Delete
```

## Flow 3: Deploy a Service
```
POST /api/deployments
  { "serviceName": "tcis_ttos_tadmin_api_uat", "triggeredBy": "admin" }
  -> Show loading spinner (can take up to 10 min for large images)
  -> On success: show output, refresh service status
  -> On failure: show errorMessage
```

## Flow 4: View Deploy History
```
GET /api/deployments/history/by-name/{serviceName}?take=10
  -> Display table: status, version, triggeredBy, startedAt, durationMs
  -> Color code: Success=green, Failed=red, InProgress=yellow
```

## Flow 5: Add New Host + Services
```
1. POST /api/hosts
     { "name": "UAT Server", "ipAddress": "192.168.100.13", "agentPort": 5155 }
2. POST /api/hosts/{newHostId}/services
     { "name": "my-service", "type": "DockerCompose",
       "composeFilePath": "/root/app/docker-compose.yml",
       "workingDirectory": "/root/app" }
3. POST /api/deployments
     { "serviceName": "my-service" }
```

## Flow 6: Batch Deploy (frontend loops)
```
// Deploy multiple services sequentially
for (const svc of selectedServices) {
  await POST /api/deployments { "serviceName": svc.name, "triggeredBy": "batch-deploy" }
}
// Or parallel if desired (backend handles concurrency per host)
```

## Flow 7: Redis Cache Management
```
// View Redis info for a host
GET /api/redis/{hostName}/info
  -> Display: version, usedMemory, totalKeys, connectedClients, uptime

// Browse keys
GET /api/redis/{hostName}/keys?pattern=cache:*&maxCount=50
  -> Display keys table with type, ttl, value preview

// View specific key
GET /api/redis/{hostName}/keys/{key}
  -> Display full value (string/list/set/hash)

// Delete cache by pattern
DELETE /api/redis/{hostName}/keys
  { "pattern": "cache:user:*" }
  -> Show deleted count

// Emergency: flush all
DELETE /api/redis/{hostName}/flush
  -> Confirm dialog first!
```

---

## 5. REDIS CACHE MANAGEMENT

**Architecture:** HostManagement.API acts as a **proxy** — it looks up the host by name/IP
in DB, resolves the agent URL (`http://{ip}:{agentPort}`), and forwards the request
to the HelperTool.API agent running on that host. The agent connects to Redis locally.

```
Frontend ? POST /api/redis/{hostName}/keys
              |
              v
   HostManagement.API (:5156)
     1. Lookup host by name/IP in DB
     2. Build agent URL: http://{host.ip}:{host.agentPort}
     3. Forward: POST http://{ip}:{port}/api/redis/keys
              |
              v
   HelperTool.API (:5155 on host)
     4. Connect to local Redis (config in appsettings)
     5. Execute command, return result
```

**Path parameter `{hostName}`:** Can be either:
- `MonitoredHost.Name` (e.g. `"UAT Server 192.168.100.13"`)
- `MonitoredHost.IpAddress` (e.g. `"192.168.100.13"`)

---

### GET /api/redis/{hostName}/info

Get Redis server info from the agent.

**Response:** `BaseResponse<RedisInfoDto>`

```typescript
interface RedisInfoDto {
  isConnected: boolean;
  redisVersion?: string;           // e.g. "7.2.4"
  usedMemory?: string;             // e.g. "1.23M"
  usedMemoryPeak?: string;         // e.g. "2.45M"
  totalKeys: number;               // keys in configured database
  connectedClients: number;
  uptimeSeconds: number;
  rawInfo: Record<string, string>; // full INFO output as key-value
}
```

**HTTP Status:** 200 OK | 404 Not Found (host not found)

---

### GET /api/redis/{hostName}/keys

Search keys by pattern.

**Query Parameters:**

| Param    | Type   | Default | Description                    |
|----------|--------|---------|--------------------------------|
| pattern  | string | `*`     | Redis SCAN pattern             |
| maxCount | int    | 100     | Max keys to return             |

**Response:** `BaseResponse<RedisKeyListDto>`

```typescript
interface RedisKeyListDto {
  pattern: string;
  count: number;
  keys: RedisKeyDto[];
}

interface RedisKeyDto {
  key: string;
  type: string;          // "String" | "List" | "Set" | "Hash" | "SortedSet" | ...
  ttl: number;           // seconds, -1 = no expiry
  value?: string;        // only for String type in search, full value in detail
}
```

**HTTP Status:** 200 OK | 404 Not Found

---

### GET /api/redis/{hostName}/keys/{key}

Get full value and metadata of a specific key.

**Response:** `BaseResponse<RedisKeyDto>`

- String ? raw value
- List ? JSON array `["a", "b"]`
- Set ? JSON array `["a", "b"]`
- Hash ? JSON object `{"field": "value"}`

**HTTP Status:** 200 OK | 404 Not Found (key or host not found)

---

### POST /api/redis/{hostName}/keys

Set a string key.

**Request Body:**

```typescript
interface SetKeyRequest {
  key: string;             // required
  value: string;           // required
  ttlSeconds?: number;     // optional TTL
}
```

**Response:** `BaseResponse<RedisKeyDto>`

**HTTP Status:** 200 OK | 400 Bad Request

---

### DELETE /api/redis/{hostName}/keys

Delete keys matching a pattern.

**Request Body:**

```typescript
interface DeleteKeysRequest {
  pattern: string;         // required - e.g. "cache:user:*"
}
```

**Response:** `BaseResponse<DeleteKeysResultDto>`

```typescript
interface DeleteKeysResultDto {
  pattern: string;
  deletedCount: number;
}
```

**HTTP Status:** 200 OK | 400 Bad Request

---

### DELETE /api/redis/{hostName}/flush

?? **DANGER** — Flush all keys in the configured Redis database.

**Response:** `BaseResponse<FlushResultDto>`

```typescript
interface FlushResultDto {
  database: number;
  message: string;
}
```

**HTTP Status:** 200 OK | 400 Bad Request

---

### Redis Error Scenarios

| Scenario | isSuccess | message |
|----------|-----------|---------||
| Host name/IP not found in DB | false | "Host '...' not found" |
| Host is inactive | false | "Host '...' (ip) is inactive" |
| Agent unreachable | false | "Agent communication failed: ..." |
| Key not found | false | "Key '...' not found" |
| Redis connection failed on agent | false | "Failed to connect to Redis: ..." |
