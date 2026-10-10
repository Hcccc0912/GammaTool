namespace Gato.Models;

public sealed class GammaProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = "New Profile";
    public bool Linked { get; set; } = true;
    public ChannelSet Channels { get; set; } = new();
    public HotkeyBinding? Hotkey { get; set; }
    public Dictionary<int, bool> MonitorEnabled { get; set; } = new() { [1] = true };
}

public sealed class ChannelSet
{
    public ChannelAdjustment Red { get; set; } = new();
    public ChannelAdjustment Green { get; set; } = new();
    public ChannelAdjustment Blue { get; set; } = new();
}

public sealed class HotkeyBinding
{
    public string Modifiers { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}
