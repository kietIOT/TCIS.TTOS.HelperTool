# ======================== BUILD ========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first for layer caching
COPY TCIS.TTOS.ToolHelper.Common/TCIS.TTOS.ToolHelper.Common.csproj TCIS.TTOS.ToolHelper.Common/
COPY TCIS.TTOS.ToolHelper.Dal/TCIS.TTOS.ToolHelper.Dal.csproj TCIS.TTOS.ToolHelper.Dal/
COPY TCIS.TTOS.HelperTool.Api/TCIS.TTOS.HelperTool.API.csproj TCIS.TTOS.HelperTool.Api/

RUN dotnet restore TCIS.TTOS.HelperTool.Api/TCIS.TTOS.HelperTool.API.csproj

# Copy all source code
COPY TCIS.TTOS.ToolHelper.Common/ TCIS.TTOS.ToolHelper.Common/
COPY TCIS.TTOS.ToolHelper.Dal/ TCIS.TTOS.ToolHelper.Dal/
COPY TCIS.TTOS.HelperTool.Api/ TCIS.TTOS.HelperTool.Api/

RUN dotnet publish TCIS.TTOS.HelperTool.Api/TCIS.TTOS.HelperTool.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ======================== RUNTIME ========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install Docker CLI & sshpass for deployment commands
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        docker.io \
        openssh-client \
        sshpass && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TCIS.TTOS.HelperTool.API.dll"]
