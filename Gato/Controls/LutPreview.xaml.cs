using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Gato.Controls;

public partial class LutPreview : UserControl
{
    public LutPreview() => InitializeComponent();

    public void SetCurves(ushort[][] curves)
    {
        if (curves.Length != 3 || curves.Any(c => c.Length != 256)) return;
        RedCurve.Points = ToPoints(curves[0]);
        GreenCurve.Points = ToPoints(curves[1]);
        BlueCurve.Points = ToPoints(curves[2]);
    }

    private static PointCollection ToPoints(ushort[] values)
    {
        var points = new PointCollection(256);
        foreach (var value in values)
        {
            var output = value / 257.0;
            points.Add(new Point(points.Count, 160.0 - output / 255.0 * 160.0));
        }
        return points;
    }
}
