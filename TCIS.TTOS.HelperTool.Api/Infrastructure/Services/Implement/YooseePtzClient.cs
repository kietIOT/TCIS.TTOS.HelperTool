using System.Net.Sockets;
using System.Text;
using TCIS.TTOS.HelperTool.API.Features.YooseeCamera;

namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Implement
{
    public sealed class YooseePtzClient(YooseeOptions opt, ILogger<YooseePtzClient> log) : IYooseePtzClient
    {
        private static readonly HashSet<string> s_actions = new(StringComparer.OrdinalIgnoreCase)
        {
            "UP", "DOWN", "LEFT", "RIGHT", "STOP", "ZOOM_IN", "ZOOM_OUT"
        };

        public IReadOnlyCollection<string> SupportedActions => s_actions;

        public async Task MoveAsync(string ip, string action, CancellationToken ct)
        {
            var normalised = action.ToUpperInvariant();
            if (!s_actions.Contains(normalised))
                throw new ArgumentException($"Unsupported PTZ action: {action}");

            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(opt.ConnectTimeout);

            await client.ConnectAsync(ip, opt.RtspPort, cts.Token);

            using var stream = client.GetStream();
            stream.ReadTimeout = (int)opt.ReadTimeout.TotalMilliseconds;
            stream.WriteTimeout = (int)opt.ReadTimeout.TotalMilliseconds;

            var setup =
                $"SETUP rtsp://{ip}/onvif1/track1 RTSP/1.0\r\n" +
                "CSeq: 1\r\n" +
                "User-Agent: LibVLC/2.2.6 (LIVE555 Streaming Media v2016.02.22)\r\n" +
                "Transport: RTP/AVP/TCP;unicast;interleaved=0-1\r\n\r\n";

            await WriteAsciiAsync(stream, setup, ct);
            _ = await ReadResponseAsync(stream, ct);

            var setParam =
                $"SET_PARAMETER rtsp://{ip}/onvif1 RTSP/1.0\r\n" +
                $"Content-type: ptzCmd: {normalised}\r\n" +
                "CSeq: 2\r\n" +
                "Session:\r\n\r\n";

            await WriteAsciiAsync(stream, setParam, ct);
            log.LogInformation("PTZ command sent: ip={Ip} action={Action}", ip, normalised);
        }

        public async Task MoveAndStopAsync(string ip, string action, CancellationToken ct)
        {
            var normalised = action.ToUpperInvariant();
            if (normalised == "STOP")
            {
                await MoveAsync(ip, "STOP", ct);
                return;
            }

            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(opt.ConnectTimeout + opt.ReadTimeout + TimeSpan.FromMilliseconds(opt.PtzStopDelayMs + 1000));

            await client.ConnectAsync(ip, opt.RtspPort, cts.Token);

            using var stream = client.GetStream();
            stream.ReadTimeout = (int)opt.ReadTimeout.TotalMilliseconds;
            stream.WriteTimeout = (int)opt.ReadTimeout.TotalMilliseconds;

            var setup =
                $"SETUP rtsp://{ip}/onvif1/track1 RTSP/1.0\r\n" +
                "CSeq: 1\r\n" +
                "User-Agent: LibVLC/2.2.6 (LIVE555 Streaming Media v2016.02.22)\r\n" +
                "Transport: RTP/AVP/TCP;unicast;interleaved=0-1\r\n\r\n";

            await WriteAsciiAsync(stream, setup, ct);
            _ = await ReadResponseAsync(stream, ct);

            var moveCmd =
                $"SET_PARAMETER rtsp://{ip}/onvif1 RTSP/1.0\r\n" +
                $"Content-type: ptzCmd: {normalised}\r\n" +
                "CSeq: 2\r\n" +
                "Session:\r\n\r\n";

            await WriteAsciiAsync(stream, moveCmd, ct);
            log.LogInformation("PTZ move sent: ip={Ip} action={Action}", ip, normalised);

            await Task.Delay(opt.PtzStopDelayMs, ct);

            var stopCmd =
                $"SET_PARAMETER rtsp://{ip}/onvif1 RTSP/1.0\r\n" +
                "Content-type: ptzCmd: STOP\r\n" +
                "CSeq: 3\r\n" +
                "Session:\r\n\r\n";

            await WriteAsciiAsync(stream, stopCmd, ct);
            log.LogInformation("PTZ stop sent: ip={Ip}", ip);
        }

        public string GetStreamUrl(string ip)
        {
            return $"rtsp://{ip}:{opt.RtspPort}/onvif1";
        }

        private static async Task WriteAsciiAsync(NetworkStream stream, string s, CancellationToken ct)
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes(s), ct);
        }

        private static async Task<string> ReadResponseAsync(NetworkStream stream, CancellationToken ct)
        {
            var buffer = new byte[4096];
            int n = await stream.ReadAsync(buffer, ct);
            return n <= 0 ? string.Empty : Encoding.ASCII.GetString(buffer, 0, n);
        }
    }
}
