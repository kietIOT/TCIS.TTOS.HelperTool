# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj để cache restore
COPY ./TCIS.TTOS.ToolHelper.Common/TCIS.TTOS.ToolHelper.Common.csproj ./TCIS.TTOS.ToolHelper.Common/
COPY ./TCIS.TTOS.HelperTool.Api/TCIS.TTOS.HelperTool.API.csproj ./TCIS.TTOS.HelperTool.Api/

RUN dotnet restore ./TCIS.TTOS.HelperTool.Api/TCIS.TTOS.HelperTool.API.csproj

# Copy full source
COPY ./TCIS.TTOS.ToolHelper.Common/ ./TCIS.TTOS.ToolHelper.Common/
COPY ./TCIS.TTOS.HelperTool.Api/ ./TCIS.TTOS.HelperTool.Api/

# Publish luôn (khỏi cần build riêng)
RUN dotnet publish ./TCIS.TTOS.HelperTool.Api/TCIS.TTOS.HelperTool.API.csproj \
    -c Release -o /app/publish /p:UseAppHost=false


# Stage 2: Runtime + Docker CLI
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Cài các dependency cần thiết
RUN apt-get update \
 && apt-get install -y ca-certificates curl gnupg \
 && install -m 0755 -d /etc/apt/keyrings \
 && curl -fsSL https://download.docker.com/linux/debian/gpg \
    | gpg --dearmor -o /etc/apt/keyrings/docker.gpg \
 && chmod a+r /etc/apt/keyrings/docker.gpg \
 && echo \
   "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
   https://download.docker.com/linux/debian \
   bookworm stable" \
   > /etc/apt/sources.list.d/docker.list \
 && apt-get update \
 && apt-get install -y docker-ce-cli docker-compose-plugin \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TCIS.TTOS.HelperTool.API.dll"]
