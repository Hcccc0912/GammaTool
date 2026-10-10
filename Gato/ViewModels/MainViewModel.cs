using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Gato.Models;
using Gato.Services;

namespace Gato.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly MonitorService _monitorService;
    private readonly GammaRampService _gammaRampService;
    private readonly HotkeyService _hotkeyService;
    private bool _syncingMonitorToggles;
    private bool _loadingSelectedValues;

    public AppConfig Config { get; }
    public ObservableCollection<GammaProfile> Profiles { get; }
    public ObservableCollection<MonitorToggleViewModel> Monitors { get; } = [];

    [ObservableProperty]
    private GammaProfile? selectedProfile;

    [ObservableProperty]
    private string selectedChannel = "Red";

    [ObservableProperty]
    private double gamma = 1.0;

    [ObservableProperty]
    private int brightness;

    [ObservableProperty]
    private double contrast = 1.0;

    public event Action<ushort[][], string>? PreviewChanged;

    public MainViewModel(ConfigService configService, MonitorService monitorService, GammaRampService gammaRampService, HotkeyService hotkeyService)
    {
        _configService = configService;
        _monitorService = monitorService;
        _gammaRampService = gammaRampService;
        _hotkeyService = hotkeyService;
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        Config = _configService.Load();
        Profiles = new ObservableCollection<GammaProfile>(Config.Profiles);
        RefreshMonitors(false);
    }

    public void InitializeSelectedProfile() =>
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == Config.LastUsedProfileId) ?? Profiles.FirstOrDefault();

    partial void OnSelectedProfileChanged(GammaProfile? value)
    {
        if (value is null) return;
        _syncingMonitorToggles = true;
        try
        {
            foreach (var monitor in Monitors)
                monitor.IsEnabled = value.MonitorEnabled.GetValueOrDefault(monitor.Index, true);
        }
        finally { _syncingMonitorToggles = false; }
        Config.LastUsedProfileId = value.Id;
        SelectedChannel = "Red";
        LoadSelectedValues();
        ApplyProfile(value);
        SaveConfig();
    }

    partial void OnSelectedChannelChanged(string value) => LoadSelectedValues();

    partial void OnGammaChanged(double value) { if (!_loadingSelectedValues) UpdateAdjustment(a => a.Gamma = value); }
    partial void OnBrightnessChanged(int value) { if (!_loadingSelectedValues) UpdateAdjustment(a => a.Brightness = value); }
    partial void OnContrastChanged(double value) { if (!_loadingSelectedValues) UpdateAdjustment(a => a.Contrast = value); }

    public void SetLinked(bool linked)
    {
        if (SelectedProfile is null || SelectedProfile.Linked == linked) return;
        SelectedProfile.Linked = linked;
        OnPropertyChanged(nameof(SelectedProfile));
        LoadSelectedValues();
        UpdatePreview();
        SaveConfig();
    }

    public void UpdateLaunchOnStartup(bool enabled)
    {
        Config.LaunchOnStartup = enabled;
        SaveConfig();
    }

    public void CreateProfile()
    {
        var profile = new GammaProfile
        {
            Id = Profiles.Count == 0 ? 1 : Profiles.Max(p => p.Id) + 1,
            Name = "New Profile",
            MonitorEnabled = Monitors.ToDictionary(m => m.Index, _ => true)
        };
        Profiles.Add(profile);
        SelectedProfile = profile;
        SaveConfig();
    }

    public GammaProfile? SaveAs(string name)
    {
        name = name.Trim();
        if (name.Length == 0 || SelectedProfile is null) return null;
        var copy = CloneProfile(SelectedProfile);
        copy.Id = Profiles.Count == 0 ? 1 : Profiles.Max(p => p.Id) + 1;
        copy.Name = name;
        copy.Hotkey = null;
        Profiles.Add(copy);
        SelectedProfile = copy;
        SaveConfig();
        return copy;
    }

    public void DeleteSelectedProfile()
    {
        if (SelectedProfile is not { } profile) return;
        _hotkeyService.Unregister(profile.Id);
        var wasCurrent = Config.LastUsedProfileId == profile.Id;
        Profiles.Remove(profile);
        Config.Profiles = Profiles.ToList();
        if (wasCurrent)
            Config.LastUsedProfileId = Profiles.FirstOrDefault()?.Id;
        SelectedProfile = wasCurrent ? Profiles.FirstOrDefault() : SelectedProfile;
        if (Profiles.Count == 0)
        {
            Config.LastUsedProfileId = null;
            SelectedProfile = null;
        }
        SaveConfig();
    }

    public void SaveSelectedProfile() => SaveConfig();

    public bool RenameSelectedProfile(string name)
    {
        name = name.Trim();
        if (name.Length == 0 || SelectedProfile is null) return false;
        if (SelectedProfile.Name == name) return true;
        SelectedProfile.Name = name;
        SaveConfig();
        return true;
    }

    public bool SetHotkey(int profileId, System.Windows.Input.ModifierKeys modifiers, System.Windows.Input.Key key)
    {
        var profile = Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile is null || !_hotkeyService.Register(profileId, modifiers, key)) return false;
        profile.Hotkey = new HotkeyBinding
        {
            Modifiers = FormatModifiers(modifiers),
            Key = key.ToString()
        };
        SaveConfig();
        return true;
    }

    public void ClearHotkey(int profileId)
    {
        _hotkeyService.Unregister(profileId);
        var profile = Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile is null) return;
        profile.Hotkey = null;
        SaveConfig();
    }

    public bool RegisterSavedHotkeys()
    {
        var allRegistered = true;
        foreach (var profile in Profiles)
        {
            if (profile.Hotkey is not { } binding ||
                !Enum.TryParse<System.Windows.Input.Key>(binding.Key, out var key) ||
                !TryParseModifiers(binding.Modifiers, out var modifiers))
                continue;
            if (!_hotkeyService.Register(profile.Id, modifiers, key)) allRegistered = false;
        }
        return allRegistered;
    }

    private void OnHotkeyPressed(int profileId)
    {
        var profile = Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile is null) return;
        SelectedProfile = profile;
        ApplyProfile(profile);
    }

    public void ResetSelectedChannel()
    {
        if (SelectedProfile is null) return;
        if (SelectedProfile.Linked)
        {
            SelectedProfile.Channels.Red = new ChannelAdjustment();
            SelectedProfile.Channels.Green = new ChannelAdjustment();
            SelectedProfile.Channels.Blue = new ChannelAdjustment();
        }
        else
        {
            SetChannel(SelectedChannel, new ChannelAdjustment());
        }
        LoadSelectedValues();
        ApplyProfile(SelectedProfile);
        UpdatePreview();
        SaveConfig();
    }

    public void ApplyProfile(GammaProfile? profile)
    {
        if (profile is null) return;
        foreach (var monitor in Monitors)
        {
            if (!monitor.IsEnabled) continue;
            _gammaRampService.Apply(monitor.Monitor,
                profile.Channels.Red, profile.Channels.Green, profile.Channels.Blue);
        }
    }

    public void ResetAllMonitors()
    {
        var defaults = new ChannelAdjustment();
        foreach (var monitor in Monitors)
            _gammaRampService.Apply(monitor.Monitor, defaults);
    }

    public void RefreshMonitors(bool save = true)
    {
        var detected = _monitorService.GetAllMonitors();
        Monitors.Clear();
        foreach (var profile in ProfilesOrEmpty())
            foreach (var monitor in detected)
                profile.MonitorEnabled.TryAdd(monitor.Index, true);
        foreach (var monitor in detected)
        {
            var enabled = SelectedProfile?.MonitorEnabled.GetValueOrDefault(monitor.Index, true) ?? true;
            var item = new MonitorToggleViewModel(monitor, enabled);
            item.PropertyChanged += (_, e) =>
            {
                if (_syncingMonitorToggles || e.PropertyName != nameof(MonitorToggleViewModel.IsEnabled)) return;
                if (SelectedProfile is { } profile)
                {
                    profile.MonitorEnabled[item.Index] = item.IsEnabled;
                    ApplyProfile(profile);
                    SaveConfig();
                }
            };
            Monitors.Add(item);
        }
        if (save) SaveConfig();
    }

    public void SetMonitorEnabled(int index, bool enabled)
    {
        if (SelectedProfile is null) return;
        SelectedProfile.MonitorEnabled[index] = enabled;
        ApplyProfile(SelectedProfile);
        SaveConfig();
    }

    public void UpdatePreview()
    {
        if (SelectedProfile is null) return;
        var curves = new[]
        {
            GetCurve(SelectedProfile.Channels.Red),
            GetCurve(SelectedProfile.Channels.Green),
            GetCurve(SelectedProfile.Channels.Blue)
        };
        var loss = curves.Select(CountDistinct).Aggregate(1L, (total, count) => total * count);
        PreviewChanged?.Invoke(curves, loss.ToString("N0"));
    }

    private IEnumerable<GammaProfile> ProfilesOrEmpty() => Profiles;

    private void LoadSelectedValues()
    {
        if (SelectedProfile is null) return;
        var adjustment = GetSelectedAdjustment();
        _loadingSelectedValues = true;
        try
        {
            Gamma = adjustment.Gamma;
            Brightness = adjustment.Brightness;
            Contrast = adjustment.Contrast;
        }
        finally { _loadingSelectedValues = false; }
        OnPropertyChanged(nameof(IsUnlinked));
        UpdatePreview();
    }

    public bool IsUnlinked => SelectedProfile is { Linked: false };

    private void UpdateAdjustment(Action<ChannelAdjustment> update)
    {
        if (SelectedProfile is null) return;
        if (SelectedProfile.Linked)
        {
            update(SelectedProfile.Channels.Red);
            update(SelectedProfile.Channels.Green);
            update(SelectedProfile.Channels.Blue);
        }
        else
        {
            update(GetSelectedAdjustment());
        }
        UpdatePreview();
        ApplyProfile(SelectedProfile);
        SaveConfig();
    }

    private ChannelAdjustment GetSelectedAdjustment() => SelectedChannel switch
    {
        "Green" => SelectedProfile!.Channels.Green,
        "Blue" => SelectedProfile!.Channels.Blue,
        _ => SelectedProfile!.Channels.Red
    };

    private void SetChannel(string channel, ChannelAdjustment adjustment)
    {
        if (SelectedProfile is null) return;
        switch (channel)
        {
            case "Green": SelectedProfile.Channels.Green = adjustment; break;
            case "Blue": SelectedProfile.Channels.Blue = adjustment; break;
            default: SelectedProfile.Channels.Red = adjustment; break;
        }
    }

    private static GammaProfile CloneProfile(GammaProfile profile) => new()
    {
        Name = profile.Name,
        Linked = profile.Linked,
        Channels = new ChannelSet
        {
            Red = CloneAdjustment(profile.Channels.Red),
            Green = CloneAdjustment(profile.Channels.Green),
            Blue = CloneAdjustment(profile.Channels.Blue)
        },
        Hotkey = profile.Hotkey is null ? null : new HotkeyBinding { Modifiers = profile.Hotkey.Modifiers, Key = profile.Hotkey.Key },
        MonitorEnabled = new Dictionary<int, bool>(profile.MonitorEnabled)
    };

    private static ChannelAdjustment CloneAdjustment(ChannelAdjustment a) => new() { Brightness = a.Brightness, Contrast = a.Contrast, Gamma = a.Gamma };

    private static ushort[] GetCurve(ChannelAdjustment adjustment)
    {
        var gamma = double.IsFinite(adjustment.Gamma) ? Math.Clamp(adjustment.Gamma, 0.5, 4.0) : 1.0;
        var contrast = double.IsFinite(adjustment.Contrast) ? Math.Clamp(adjustment.Contrast, 0.5, 2.0) : 1.0;
        var brightness = Math.Clamp(adjustment.Brightness, -128, 128);
        var curve = new ushort[256];
        for (var i = 0; i < 256; i++)
        {
            var value = Math.Pow(i / 255.0, 1.0 / gamma) * 255.0;
            value = Math.Clamp((value - 128.0) * contrast + 128.0 + brightness, 0.0, 255.0);
            curve[i] = (ushort)(value * 257.0);
        }
        return curve;
    }

    private static int CountDistinct(ushort[] curve)
    {
        var duplicates = 0;
        for (var i = 0; i < curve.Length - 1; i++)
            if (curve[i] == curve[i + 1]) duplicates++;
        return 256 - duplicates;
    }

    private void SaveConfig()
    {
        Config.Profiles = Profiles.ToList();
        _configService.Save(Config);
    }

    private static string FormatModifiers(System.Windows.Input.ModifierKeys modifiers) => string.Join("+", new[]
    {
        (System.Windows.Input.ModifierKeys.Control, "Ctrl"),
        (System.Windows.Input.ModifierKeys.Alt, "Alt"),
        (System.Windows.Input.ModifierKeys.Shift, "Shift"),
        (System.Windows.Input.ModifierKeys.Windows, "Win")
    }.Where(item => (modifiers & item.Item1) != 0).Select(item => item.Item2));

    private static bool TryParseModifiers(string value, out System.Windows.Input.ModifierKeys modifiers)
    {
        modifiers = System.Windows.Input.ModifierKeys.None;
        foreach (var part in value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            modifiers |= part switch
            {
                "Ctrl" => System.Windows.Input.ModifierKeys.Control,
                "Alt" => System.Windows.Input.ModifierKeys.Alt,
                "Shift" => System.Windows.Input.ModifierKeys.Shift,
                "Win" => System.Windows.Input.ModifierKeys.Windows,
                _ => System.Windows.Input.ModifierKeys.None
            };
        }
        return modifiers != System.Windows.Input.ModifierKeys.None;
    }
}
