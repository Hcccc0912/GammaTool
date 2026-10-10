using CommunityToolkit.Mvvm.ComponentModel;
using Gato.Models;

namespace Gato.ViewModels;

public partial class MonitorToggleViewModel : ObservableObject
{
    public MonitorInfo Monitor { get; }
    public int Index => Monitor.Index;
    public string Label => $"Monitor {Monitor.Index} - {Monitor.Width}x{Monitor.Height}";

    [ObservableProperty]
    private bool isEnabled;

    public MonitorToggleViewModel(MonitorInfo monitor, bool isEnabled)
    {
        Monitor = monitor;
        this.isEnabled = isEnabled;
    }
}
