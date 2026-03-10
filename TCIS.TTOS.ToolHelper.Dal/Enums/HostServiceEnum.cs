namespace TCIS.TTOS.ToolHelper.Dal.Enums
{
    public enum HostStatus
    {
        Online,
        Offline,
        Degraded,
        Unknown
    }

    public enum ServiceStatus
    {
        Running,
        Stopped,
        Error,
        Unknown
    }

    public enum ServiceType
    {
        DockerCompose,
        DockerContainer,
        Systemd,
        WindowsService,
        WebApp,
        Database,
        Other
    }

    public enum DeploymentStatus
    {
        Pending,
        InProgress,
        Success,
        Failed,
        RolledBack
    }
}
