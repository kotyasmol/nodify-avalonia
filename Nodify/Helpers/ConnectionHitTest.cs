namespace Nodify;

internal static class ConnectionHitTest
{
    public static bool Intersects(Control control, Visual relativeTo, Point start, Point end)
    {
        var transform = control.TransformToVisual(relativeTo);
        if (!transform.HasValue)
            return false;

        Geometry? geometry;
        if (control is WpfShape connection)
            geometry = connection.GetHitGeometry();
        else if (control is Shape shape && shape.RenderedGeometry is { } rendered)
        {
            var group = new GeometryGroup { FillRule = FillRule.NonZero };
            if (shape.Fill != null)
                group.Children.Add(rendered);
            if (shape.Stroke != null && shape.StrokeThickness > 0)
                group.Children.Add(rendered.GetWidenedGeometry(new Pen(shape.Stroke, shape.StrokeThickness,
                    shape.StrokeDashArray == null ? null : new DashStyle(shape.StrokeDashArray, shape.StrokeDashOffset),
                    shape.StrokeLineCap, shape.StrokeJoin)));
            geometry = group;
        }
        else
            geometry = new RectangleGeometry(new Rect(control.Bounds.Size));

        if (geometry == null)
            return false;

        var delta = end - start;
        double length = delta.Length();
        if (length == 0)
            return false;

        // Rotate the cut to the X axis. Avalonia 12.0 can intersect paths with rectangles.
        var toCut = Matrix.CreateTranslation(-start.X, -start.Y) *
                    Matrix.CreateRotation(-Math.Atan2(delta.Y, delta.X));
        var transformed = new GeometryGroup
        {
            Children = { geometry },
            Transform = new MatrixTransform(transform.Value * toCut)
        };
        var cut = new Rect(0, -0.5, length, 1);
        if (!transformed.Bounds.Intersects(cut))
            return false;
        var intersection = Geometry.Combine(transformed, new RectangleGeometry(cut), GeometryCombineMode.Intersect);
        return intersection.Bounds.Width > 0 && intersection.Bounds.Height > 0;
    }
}
