namespace TCIS.TTOS.HelperTool.API.Features.YooseeCamera;

public sealed class YooseeOptions
{
    public string TokenFilePath { get; set; } = "./token.txt";
    public int RtspPort { get; set; } = 554;
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public int PtzStopDelayMs { get; set; } = 500;
}
