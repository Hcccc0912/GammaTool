using System.Runtime.InteropServices;

namespace Gato.Interop;

internal static class NativeMethods
{
    internal const uint DisplayDeviceAttachedToDesktop = 0x00000001;
    internal const uint DisplayDeviceMirroringDriver = 0x00000008;
    internal const int CchDeviceName = 32;
    internal const int CchDeviceString = 128;
    internal const int CchFormName = 32;
    internal const int EnumCurrentSettings = -1;

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr CreateDC(string? driver, string device, string? output, IntPtr initData);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetDeviceGammaRamp(IntPtr hdc, [In] ushort[] ramp);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetDeviceGammaRamp(IntPtr hdc, [Out] ushort[] ramp);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumDisplayDevices(string? device, uint deviceNumber, ref DisplayDevice displayDevice, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumDisplaySettings(string deviceName, int modeNumber, ref DevMode devMode);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern IntPtr FindWindow(string? className, string? windowName);

    [DllImport("user32.dll")]
    internal static extern bool ShowWindow(IntPtr hWnd, int command);

    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DisplayDevice
    {
        internal int Size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchDeviceName)] internal string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchDeviceString)] internal string DeviceString;
        internal uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchDeviceString)] internal string DeviceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchDeviceString)] internal string DeviceKey;

        internal static DisplayDevice Create() => new()
        {
            Size = Marshal.SizeOf<DisplayDevice>(),
            DeviceName = string.Empty,
            DeviceString = string.Empty,
            DeviceId = string.Empty,
            DeviceKey = string.Empty
        };
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchDeviceName)] internal string DeviceName;
        internal ushort SpecVersion;
        internal ushort DriverVersion;
        internal ushort Size;
        internal ushort DriverExtra;
        internal uint Fields;
        internal short Orientation;
        internal short PaperSize;
        internal short PaperLength;
        internal short PaperWidth;
        internal short Scale;
        internal short Copies;
        internal short DefaultSource;
        internal short PrintQuality;
        internal short Color;
        internal short Duplex;
        internal short YResolution;
        internal short TTOption;
        internal short Collate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchFormName)] internal string FormName;
        internal ushort LogPixels;
        internal uint BitsPerPel;
        internal uint PelsWidth;
        internal uint PelsHeight;
        internal uint DisplayFlags;
        internal uint DisplayFrequency;
        internal uint ICMMethod;
        internal uint ICMIntent;
        internal uint MediaType;
        internal uint DitherType;
        internal uint Reserved1;
        internal uint Reserved2;
        internal uint PanningWidth;
        internal uint PanningHeight;

        internal static DevMode Create() => new()
        {
            DeviceName = string.Empty,
            FormName = string.Empty,
            Size = (ushort)Marshal.SizeOf<DevMode>()
        };
    }
}
