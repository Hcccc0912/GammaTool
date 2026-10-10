namespace Gato.Models;

public sealed class ChannelAdjustment
{
    public int Brightness { get; set; }
    public double Contrast { get; set; } = 1.0;
    public double Gamma { get; set; } = 1.0;
}
