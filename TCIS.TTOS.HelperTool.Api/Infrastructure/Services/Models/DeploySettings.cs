namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Models
{
    public class DeploySettings
    {
        public List<DeployJob> Jobs { get; set; } = new();
    }

    public class DeployJob
    {
        public string Name { get; set; } = string.Empty;
        public string ComposeFile { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public Dictionary<string, string>? EnvironmentVariables { get; set; }
    }
}
