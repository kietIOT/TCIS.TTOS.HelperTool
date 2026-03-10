using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TCIS.TTOS.ToolHelper.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddDeploymentSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "monitored_hosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SshPort = table.Column<int>(type: "integer", nullable: true),
                    SshUsername = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SshPrivateKeyPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SshPassword = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Os = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastCheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monitored_hosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "monitored_services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: true),
                    HealthCheckUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ImageName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Version = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ComposeFilePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    WorkingDirectory = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DockerfilePath = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContainerName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DeployCommand = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    StopCommand = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    RestartCommand = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    LastDeploymentStatus = table.Column<int>(type: "integer", nullable: true),
                    LastDeployedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastCheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monitored_services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_monitored_services_monitored_hosts_HostId",
                        column: x => x.HostId,
                        principalTable: "monitored_hosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deployment_histories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TriggeredBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Output = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deployment_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deployment_histories_monitored_services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "monitored_services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_deployment_histories_service_started",
                table: "deployment_histories",
                columns: new[] { "ServiceId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_deployment_histories_status",
                table: "deployment_histories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_monitored_hosts_active_status",
                table: "monitored_hosts",
                columns: new[] { "IsActive", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_monitored_hosts_ip",
                table: "monitored_hosts",
                column: "IpAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_monitored_services_host_active",
                table: "monitored_services",
                columns: new[] { "HostId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_monitored_services_host_name",
                table: "monitored_services",
                columns: new[] { "HostId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_monitored_services_name",
                table: "monitored_services",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "ix_monitored_services_type",
                table: "monitored_services",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deployment_histories");

            migrationBuilder.DropTable(
                name: "monitored_services");

            migrationBuilder.DropTable(
                name: "monitored_hosts");
        }
    }
}
