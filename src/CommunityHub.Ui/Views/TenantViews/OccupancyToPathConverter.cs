using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CommunityHub.Ui.Views.TenantViews;

public class OccupancyToPathConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int percentage)
            return null!;

        // Donut chart parameters
        const double radius = 55;
        const double centerX = 65;
        const double centerY = 65;

        // Convert percentage to angle (0-360 degrees)
        double angle = (percentage / 100.0) * 360.0;
        double angleRadians = angle * Math.PI / 180.0;

        // Calculate end point on the circle
        double endX = centerX + radius * Math.Sin(angleRadians);
        double endY = centerY - radius * Math.Cos(angleRadians);

        // Large arc flag (1 if angle > 180, 0 otherwise)
        int largeArc = angle > 180 ? 1 : 0;

        // Create PathGeometry
        PathGeometry geometry = new PathGeometry();
        PathFigure figure = new PathFigure();
        figure.StartPoint = new System.Windows.Point(centerX, centerY - radius);
        figure.IsClosed = false;

        ArcSegment arc = new ArcSegment();
        arc.Point = new System.Windows.Point(endX, endY);
        arc.Size = new System.Windows.Size(radius, radius);
        arc.IsLargeArc = angle > 180;
        arc.SweepDirection = SweepDirection.Clockwise;

        figure.Segments.Add(arc);
        geometry.Figures.Add(figure);

        return geometry;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
