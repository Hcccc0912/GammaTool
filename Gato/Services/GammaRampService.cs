using Gato.Interop;
using Gato.Models;

namespace Gato.Services;

public sealed class GammaRampService
{
    public bool Apply(MonitorInfo monitor, ChannelAdjustment uniform) => Apply(monitor, uniform, uniform, uniform);

    public bool Apply(MonitorInfo monitor, ChannelAdjustment red, ChannelAdjustment green, ChannelAdjustment blue)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(red);
        ArgumentNullException.ThrowIfNull(green);
        ArgumentNullException.ThrowIfNull(blue);

        var ramp = new ushort[768];
        ComputeChannelCurve(red).CopyTo(ramp, 0);
        ComputeChannelCurve(green).CopyTo(ramp, 256);
        ComputeChannelCurve(blue).CopyTo(ramp, 512);

        var hdc = NativeMethods.CreateDC(null, monitor.DeviceName, null, IntPtr.Zero);
        if (hdc == IntPtr.Zero)
            return false;

        try
        {
            return NativeMethods.SetDeviceGammaRamp(hdc, ramp);
        }
        finally
        {
            NativeMethods.DeleteDC(hdc);
        }
    }

    private static ushort[] ComputeChannelCurve(ChannelAdjustment adjustment)
    {
        var brightness = Math.Clamp(adjustment.Brightness, -128, 128);
        var contrast = ClampFinite(adjustment.Contrast, 0.5, 2.0, 1.0);
        var gamma = ClampFinite(adjustment.Gamma, 0.5, 4.0, 1.0);
        var curve = new ushort[256];

        for (var i = 0; i < curve.Length; i++)
        {
            var value = Math.Pow(i / 255.0, 1.0 / gamma) * 255.0;
            value = (value - 128.0) * contrast + 128.0 + brightness;
            value = Math.Clamp(value, 0.0, 255.0);
            curve[i] = (ushort)(value * 257.0);
        }

        return curve;
    }

    private static double ClampFinite(double value, double minimum, double maximum, double fallback) =>
        double.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;
}
