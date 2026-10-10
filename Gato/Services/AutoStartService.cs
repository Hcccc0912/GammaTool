using Microsoft.Win32;

namespace Gato.Services;

public sealed class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Gato";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string value && value.Contains("--silent", StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (enabled)
            key.SetValue(ValueName, $"\"{Environment.ProcessPath ?? System.Reflection.Assembly.GetEntryAssembly()!.Location}\" --silent");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
