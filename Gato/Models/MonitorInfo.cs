namespace Gato.Models;

public sealed record MonitorInfo
{
    public int Index { get; init; }
    public string DeviceName { get; init; } = string.Empty;
    public string FriendlyName { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
}
