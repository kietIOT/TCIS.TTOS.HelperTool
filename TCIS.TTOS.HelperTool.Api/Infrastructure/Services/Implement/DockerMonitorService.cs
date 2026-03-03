using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Models;

namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Implement
{
    public class DockerMonitorService(
        IOptions<DockerMonitorOptions> options,
        ILogger<DockerMonitorService> logger) : IDockerMonitorService
    {
        private readonly DockerMonitorOptions _options = options.Value;

        // Track consecutive spike counts per container
        private static readonly ConcurrentDictionary<string, int> s_cpuSpikeCounter = new();
        private static readonly ConcurrentDictionary<string, int> s_memSpikeCounter = new();
        private static readonly ConcurrentDictionary<string, double> s_previousCpu = new();

        public async Task<BaseResponse<object>> GetContainerStatsAsync(CancellationToken ct = default)
        {
            var stats = await CollectContainerStatsAsync(ct);
            return new BaseResponse<object>
            {
                IsSuccess = true,
                Data = stats,
                Message = $"{stats.Count} container(s) running"
            };
        }

        public async Task<BaseResponse<object>> GetDiskUsageAsync(CancellationToken ct = default)
        {
            var disks = await CollectDiskUsageAsync(ct);
            return new BaseResponse<object>
            {
                IsSuccess = true,
                Data = disks,
                Message = $"{disks.Count} filesystem(s)"
            };
        }

        public async Task<BaseResponse<object>> GetFullReportAsync(CancellationToken ct = default)
        {
            var statsTask = CollectContainerStatsAsync(ct);
            var diskTask = CollectDiskUsageAsync(ct);
            await Task.WhenAll(statsTask, diskTask);

            return new BaseResponse<object>
            {
                IsSuccess = true,
                Data = new
                {
                    Containers = statsTask.Result,
                    Disks = diskTask.Result,
                    Timestamp = DateTimeOffset.UtcNow
                },
                Message = "Full host report"
            };
        }

        public async Task MonitorAndAlertAsync(CancellationToken ct = default)
        {
            var stats = await CollectContainerStatsAsync(ct);
            var disks = await CollectDiskUsageAsync(ct);

            CheckContainerAlerts(stats);
            CheckDiskAlerts(disks);
            PrintSummary(stats, disks);
        }

        #region Docker Stats

        private static async Task<List<ContainerStat>> CollectContainerStatsAsync(CancellationToken ct)
        {
            // docker stats --no-stream --format "{{.Name}}|{{.CPUPerc}}|{{.MemUsage}}|{{.MemPerc}}|{{.NetIO}}|{{.BlockIO}}|{{.PIDs}}"
            var output = await RunCommandAsync(
                "docker stats --no-stream --format \"{{.Name}}|{{.CPUPerc}}|{{.MemUsage}}|{{.MemPerc}}|{{.NetIO}}|{{.BlockIO}}|{{.PIDs}}\"",
                ct);

            var results = new List<ContainerStat>();
            if (string.IsNullOrWhiteSpace(output)) return results;

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Trim().Split('|');
                if (parts.Length < 7) continue;

                results.Add(new ContainerStat
                {
                    Name = parts[0],
                    CpuPercent = ParsePercent(parts[1]),
                    MemUsage = parts[2].Trim(),
                    MemPercent = ParsePercent(parts[3]),
                    NetIO = parts[4].Trim(),
                    BlockIO = parts[5].Trim(),
                    Pids = int.TryParse(parts[6].Trim(), out var p) ? p : 0
                });
            }

            return results;
        }

        #endregion

        #region Disk Usage

        private async Task<List<DiskInfo>> CollectDiskUsageAsync(CancellationToken ct)
        {
            // Works on Linux (Docker host)
            var output = await RunCommandAsync("df -h --output=source,size,used,avail,pcent,target 2>/dev/null || wmic logicaldisk get size,freespace,caption 2>&1", ct);

            var results = new List<DiskInfo>();
            if (string.IsNullOrWhiteSpace(output)) return results;

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Detect if it's Linux df output (has % sign)
            if (output.Contains('%'))
            {
                // Linux df -h output
                foreach (var line in lines.Skip(1)) // skip header
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 6) continue;

                    // Skip pseudo filesystems
                    var source = parts[0];
                    if (source.StartsWith("tmpfs") || source.StartsWith("devtmpfs") ||
                        source.StartsWith("shm") || source == "overlay" ||
                        source.StartsWith("udev") || source == "none")
                        continue;

                    results.Add(new DiskInfo
                    {
                        Filesystem = source,
                        Size = parts[1],
                        Used = parts[2],
                        Available = parts[3],
                        UsePercent = ParsePercent(parts[4]),
                        MountPoint = parts[5]
                    });
                }
            }
            else
            {
                // Windows wmic output fallback
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3) continue;

                    var caption = parts[0];
                    if (!long.TryParse(parts[1], out var freeSpace)) continue;
                    if (!long.TryParse(parts[2], out var totalSize)) continue;
                    if (totalSize == 0) continue;

                    var usedSpace = totalSize - freeSpace;
                    var usePercent = (double)usedSpace / totalSize * 100;

                    results.Add(new DiskInfo
                    {
                        Filesystem = caption,
                        Size = FormatBytes(totalSize),
                        Used = FormatBytes(usedSpace),
                        Available = FormatBytes(freeSpace),
                        UsePercent = Math.Round(usePercent, 1),
                        MountPoint = caption
                    });
                }
            }

            return results;
        }

        #endregion

        #region Alert Logic

        private void CheckContainerAlerts(List<ContainerStat> stats)
        {
            foreach (var stat in stats)
            {
                // CPU spike detection
                var cpuCount = s_cpuSpikeCounter.GetOrAdd(stat.Name, 0);
                if (stat.CpuPercent >= _options.CpuSpikeThresholdPercent)
                {
                    cpuCount++;
                    s_cpuSpikeCounter[stat.Name] = cpuCount;

                    if (cpuCount >= _options.SpikeConsecutiveCount)
                    {
                        PrintCpuAlert(stat, cpuCount);
                    }
                }
                else
                {
                    // Reset if back to normal, but log recovery
                    if (cpuCount >= _options.SpikeConsecutiveCount)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[MONITOR] ? CPU recovered: {stat.Name} ? {stat.CpuPercent:F1}%");
                        Console.ResetColor();
                    }
                    s_cpuSpikeCounter[stat.Name] = 0;
                }

                // Track CPU trend
                if (s_previousCpu.TryGetValue(stat.Name, out var prevCpu))
                {
                    var delta = stat.CpuPercent - prevCpu;
                    if (delta > 30) // Jump > 30% in one poll
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[MONITOR] ? CPU sudden jump: {stat.Name} {prevCpu:F1}% ? {stat.CpuPercent:F1}% (?{delta:+F1}%)");
                        Console.ResetColor();
                    }
                }
                s_previousCpu[stat.Name] = stat.CpuPercent;

                // Memory spike detection
                var memCount = s_memSpikeCounter.GetOrAdd(stat.Name, 0);
                if (stat.MemPercent >= _options.MemSpikeThresholdPercent)
                {
                    memCount++;
                    s_memSpikeCounter[stat.Name] = memCount;

                    if (memCount >= _options.SpikeConsecutiveCount)
                    {
                        PrintMemAlert(stat, memCount);
                    }
                }
                else
                {
                    if (memCount >= _options.SpikeConsecutiveCount)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[MONITOR] ? Memory recovered: {stat.Name} ? {stat.MemPercent:F1}%");
                        Console.ResetColor();
                    }
                    s_memSpikeCounter[stat.Name] = 0;
                }
            }
        }

        private void CheckDiskAlerts(List<DiskInfo> disks)
        {
            foreach (var disk in disks)
            {
                if (disk.UsePercent >= _options.DiskCriticalThresholdPercent)
                {
                    PrintDiskAlert(disk, isCritical: true);
                }
                else if (disk.UsePercent >= _options.DiskWarningThresholdPercent)
                {
                    PrintDiskAlert(disk, isCritical: false);
                }
            }
        }

        #endregion

        #region Console Output

        private static void PrintCpuAlert(ContainerStat stat, int consecutiveCount)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("????????????????????????????????????????????????????????????????");
            Console.WriteLine($"?  ?? CPU SPIKE ALERT                                        ?");
            Console.WriteLine($"?  Container: {stat.Name,-48}?");
            Console.WriteLine($"?  CPU: {stat.CpuPercent,6:F1}%   Memory: {stat.MemPercent,5:F1}%                       ?");
            Console.WriteLine($"?  Consecutive spikes: {consecutiveCount,-39}?");
            Console.WriteLine($"?  PIDs: {stat.Pids,-53}?");
            Console.WriteLine("????????????????????????????????????????????????????????????????");
            Console.ResetColor();
        }

        private static void PrintMemAlert(ContainerStat stat, int consecutiveCount)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("????????????????????????????????????????????????????????????????");
            Console.WriteLine($"?  ?? MEMORY SPIKE ALERT                                     ?");
            Console.WriteLine($"?  Container: {stat.Name,-48}?");
            Console.WriteLine($"?  Memory: {stat.MemPercent,5:F1}%  ({stat.MemUsage})                   ?");
            Console.WriteLine($"?  Consecutive spikes: {consecutiveCount,-39}?");
            Console.WriteLine("????????????????????????????????????????????????????????????????");
            Console.ResetColor();
        }

        private static void PrintDiskAlert(DiskInfo disk, bool isCritical)
        {
            Console.WriteLine();
            Console.ForegroundColor = isCritical ? ConsoleColor.Red : ConsoleColor.Yellow;
            var level = isCritical ? "?? CRITICAL" : "??  WARNING";
            Console.WriteLine("????????????????????????????????????????????????????????????????");
            Console.WriteLine($"?  ?? DISK {level}                                    ?");
            Console.WriteLine($"?  Mount: {disk.MountPoint,-52}?");
            Console.WriteLine($"?  Used: {disk.UsePercent,5:F1}%  ({disk.Used} / {disk.Size})              ?");
            Console.WriteLine($"?  Available: {disk.Available,-48}?");
            Console.WriteLine("????????????????????????????????????????????????????????????????");
            Console.ResetColor();
        }

        private static void PrintSummary(List<ContainerStat> stats, List<DiskInfo> disks)
        {
            if (stats.Count == 0 && disks.Count == 0) return;

            var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[MONITOR] {now:HH:mm:ss} | Containers: {stats.Count}");

            if (stats.Count > 0)
            {
                foreach (var s in stats.OrderByDescending(x => x.CpuPercent))
                {
                    var cpuBar = BuildBar(s.CpuPercent, 20);
                    var memBar = BuildBar(s.MemPercent, 15);

                    var cpuColor = s.CpuPercent >= 80 ? ConsoleColor.Red :
                                   s.CpuPercent >= 50 ? ConsoleColor.Yellow : ConsoleColor.Green;
                    var memColor = s.MemPercent >= 80 ? ConsoleColor.Red :
                                   s.MemPercent >= 50 ? ConsoleColor.Yellow : ConsoleColor.Green;

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"  {s.Name,-30} CPU:");
                    Console.ForegroundColor = cpuColor;
                    Console.Write($"{cpuBar} {s.CpuPercent,5:F1}%");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("  MEM:");
                    Console.ForegroundColor = memColor;
                    Console.Write($"{memBar} {s.MemPercent,5:F1}%");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  ({s.MemUsage})");
                }
            }

            if (disks.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"           Disks:");
                foreach (var d in disks)
                {
                    var diskBar = BuildBar(d.UsePercent, 20);
                    var diskColor = d.UsePercent >= 95 ? ConsoleColor.Red :
                                    d.UsePercent >= 85 ? ConsoleColor.Yellow : ConsoleColor.Green;

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"  {d.MountPoint,-30} ");
                    Console.ForegroundColor = diskColor;
                    Console.Write($"{diskBar} {d.UsePercent,5:F1}%");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  ({d.Used}/{d.Size}, free: {d.Available})");
                }
            }

            Console.ResetColor();
        }

        private static string BuildBar(double percent, int width)
        {
            var filled = (int)(percent / 100 * width);
            if (filled > width) filled = width;
            return "[" + new string('?', filled) + new string('?', width - filled) + "]";
        }

        #endregion

        #region Helpers

        private static async Task<string> RunCommandAsync(string command, CancellationToken ct)
        {
            var isWindows = OperatingSystem.IsWindows();
            var startInfo = new ProcessStartInfo
            {
                FileName = isWindows ? "cmd.exe" : "/bin/bash",
                Arguments = isWindows ? $"/c {command}" : $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            var output = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) output.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();

            await process.WaitForExitAsync(ct);
            return output.ToString();
        }

        private static double ParsePercent(string value)
        {
            var cleaned = value.Trim().TrimEnd('%');
            return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0;
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
            int i = 0;
            double size = bytes;
            while (size >= 1024 && i < suffixes.Length - 1) { size /= 1024; i++; }
            return $"{size:F1}{suffixes[i]}";
        }

        #endregion

        #region DTOs

        public sealed class ContainerStat
        {
            public string Name { get; init; } = default!;
            public double CpuPercent { get; init; }
            public string MemUsage { get; init; } = default!;
            public double MemPercent { get; init; }
            public string NetIO { get; init; } = default!;
            public string BlockIO { get; init; } = default!;
            public int Pids { get; init; }
        }

        public sealed class DiskInfo
        {
            public string Filesystem { get; init; } = default!;
            public string Size { get; init; } = default!;
            public string Used { get; init; } = default!;
            public string Available { get; init; } = default!;
            public double UsePercent { get; init; }
            public string MountPoint { get; init; } = default!;
        }

        #endregion
    }
}
