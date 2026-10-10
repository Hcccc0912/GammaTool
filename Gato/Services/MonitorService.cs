using Gato.Interop;
using Gato.Models;

namespace Gato.Services;

public sealed class MonitorService
{
    public List<MonitorInfo> GetAllMonitors()
    {
        var monitors = new List<MonitorInfo>();

        for (uint deviceNumber = 0; ; deviceNumber++)
        {
            var device = NativeMethods.DisplayDevice.Create();
            if (!NativeMethods.EnumDisplayDevices(null, deviceNumber, ref device, 0))
                break;

            if ((device.StateFlags & NativeMethods.DisplayDeviceAttachedToDesktop) == 0 ||
                (device.StateFlags & NativeMethods.DisplayDeviceMirroringDriver) != 0)
                continue;

            var mode = NativeMethods.DevMode.Create();
            var hasMode = NativeMethods.EnumDisplaySettings(device.DeviceName, NativeMethods.EnumCurrentSettings, ref mode);
            var friendlyName = device.DeviceString;
            var physicalMonitor = NativeMethods.DisplayDevice.Create();
            if (NativeMethods.EnumDisplayDevices(device.DeviceName, 0, ref physicalMonitor, 0) &&
                !string.IsNullOrWhiteSpace(physicalMonitor.DeviceString))
                friendlyName = physicalMonitor.DeviceString;

            monitors.Add(new MonitorInfo
            {
                Index = monitors.Count + 1,
                DeviceName = device.DeviceName,
                FriendlyName = friendlyName,
                Width = hasMode ? checked((int)mode.PelsWidth) : 0,
                Height = hasMode ? checked((int)mode.PelsHeight) : 0
            });
        }

        return monitors;
    }
}
