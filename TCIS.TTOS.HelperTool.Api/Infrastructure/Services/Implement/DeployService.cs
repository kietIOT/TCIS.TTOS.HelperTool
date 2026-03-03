using Microsoft.Extensions.Options;
using System.Diagnostics;
using TCIS.TTOS.HelperTool.API.Features.Deploy;

namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Implement
{
    public class DeployService(IOptions<DeploySettings> settings) : IDeployService
    {
        private readonly DeploySettings _settings = settings.Value;

        public async Task<DeployResponse> DeployAsync(string jobName, string? environment)
        {
            var job = _settings.Jobs.FirstOrDefault(j => j.Name.Equals(jobName, StringComparison.OrdinalIgnoreCase));

            if (job == null)
            {
                return new DeployResponse
                {
                    Success = false,
                    Message = $"Job '{jobName}' not found in configuration"
                };
            }

            if (!File.Exists(job.ComposeFile))
            {
                return new DeployResponse
                {
                    Success = false,
                    Message = $"Compose file not found: {job.ComposeFile}"
                };
            }

            try
            {
                // Step 1: Stop containers and remove images
                var downResult = await ExecuteDockerCommand(
                    $"docker compose -f {job.ComposeFile} down --rmi all",
                    job.WorkingDirectory,
                    job.EnvironmentVariables,
                    environment
                );

                if (!downResult.Success)
                {
                    // Continue anyway, down might fail if containers don't exist yet
                }

                // Step 2: Start containers (will pull latest images automatically)
                var upResult = await ExecuteDockerCommand(
                    $"docker compose  -f {job.ComposeFile} up -d",
                    job.WorkingDirectory,
                    job.EnvironmentVariables,
                    environment
                );

                if (!upResult.Success)
                {
                    return upResult;
                }

                return new DeployResponse
                {
                    Success = true,
                    Message = "Deployment completed successfully",
                    Output = $"Down output:{downResult.Output} Up output:{upResult.Output}"
                };
            }
            catch (Exception ex)
            {
                return new DeployResponse
                {
                    Success = false,
                    Message = "Deployment failed",
                    Error = ex.Message
                };
            }
        }

        public List<string> GetAvailableJobs()
        {
            return _settings.Jobs.Select(j => j.Name).ToList();
        }

        private static async Task<DeployResponse> ExecuteDockerCommand(
            string command,
            string workingDirectory,
            Dictionary<string, string>? envVars,
            string? environment)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Add environment variables
            if (envVars != null)
            {
                foreach (var kvp in envVars)
                {
                    startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }

            if (!string.IsNullOrEmpty(environment))
            {
                startInfo.EnvironmentVariables["DEPLOY_ENV"] = environment;
            }

            using var process = new Process { StartInfo = startInfo };

            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var success = process.ExitCode == 0;
            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            return new DeployResponse
            {
                Success = success,
                Message = success ? "Command executed successfully" : "Command failed",
                Output = output,
                Error = string.IsNullOrEmpty(error) ? null : error
            };
        }
    }
}
