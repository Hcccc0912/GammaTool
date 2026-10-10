namespace Gato.Models;

public sealed class AppConfig
{
    public int? LastUsedProfileId { get; set; }
    public bool LaunchOnStartup { get; set; }
    public List<GammaProfile> Profiles { get; set; } = [];

    public static AppConfig CreateDefault() => new()
    {
        LastUsedProfileId = 1,
        Profiles =
        [
            new GammaProfile
            {
                Id = 1,
                Name = "Default",
                Linked = true,
                Channels = new ChannelSet(),
                MonitorEnabled = new Dictionary<int, bool> { [1] = true }
            }
        ]
    };
}
