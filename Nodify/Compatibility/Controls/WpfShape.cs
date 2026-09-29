namespace Nodify.Compatibility;

/// <summary>
/// Nodify wants to override Render method in Shape, but this is not legal in Avalonia.
/// This control is a workaround to allow overriding Render method in a Shape.
/// A Shape supplies layout and cached geometry; a single visual renders the connection.
/// </summary>
public class WpfShape : Panel
{
    private InnerShape? _shape;
    private InnerRenderer? _renderer;
    private Geometry? _hitGeometry;

    public static readonly StyledProperty<IBrush?> FillProperty = Shape.FillProperty.AddOwner<WpfShape>();

    public static readonly StyledProperty<Stretch> StretchProperty = Shape.StretchProperty.AddOwner<WpfShape>();
    
    public static readonly StyledProperty<IBrush?> StrokeProperty = Shape.StrokeProperty.AddOwner<WpfShape>();
    
    public static readonly StyledProperty<AvaloniaList<double>?> StrokeDashArrayProperty = Shape.StrokeDashArrayProperty.AddOwner<WpfShape>();
    
    public static readonly StyledProperty<double> StrokeDashOffsetProperty = Shape.StrokeDashOffsetProperty.AddOwner<WpfShape>();
    
    public static readonly StyledProperty<double> StrokeThicknessProperty = Shape.StrokeThicknessProperty.AddOwner<WpfShape>();
    
    public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty = Shape.StrokeLineCapProperty.AddOwner<WpfShape>();
    
    public static readonly StyledProperty<PenLineJoin> StrokeJoinProperty = Shape.StrokeJoinProperty.AddOwner<WpfShape>();
    
    protected static void AffectsGeometry<TShape>(params AvaloniaProperty[] properties) where TShape : WpfShape
    {
        foreach (AvaloniaProperty property in properties)
            property.Changed.AddClassHandler<TShape>(AffectsGeometryInvalidate);
    }
    
    protected new static void AffectsRender<T>(params AvaloniaProperty[] properties) where T : WpfShape
    {
        foreach (AvaloniaProperty property in properties)
            property.Changed.AddClassHandler<T>((control, e) => control.InvalidateVisual());
    }
    
    private static void AffectsGeometryInvalidate(WpfShape control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty && ((Rect) e.OldValue).Size == ((Rect) e.NewValue).Size)
            return;
        
        control.InvalidateGeometry();
    }

    public WpfShape()
    {
        _shape = new InnerShape();
        _shape.Bind(StretchProperty, this.GetObservable(StretchProperty), BindingPriority.Template);
        _shape.Bind(FillProperty, this.GetObservable(FillProperty), BindingPriority.Template);
        _shape.Bind(StrokeProperty, this.GetObservable(StrokeProperty), BindingPriority.Template);
        _shape.Bind(StrokeThicknessProperty, this.GetObservable(StrokeThicknessProperty), BindingPriority.Template);
        _shape.Bind(StrokeDashArrayProperty, this.GetObservable(StrokeDashArrayProperty), BindingPriority.Template);
        _shape.Bind(StrokeDashOffsetProperty, this.GetObservable(StrokeDashOffsetProperty), BindingPriority.Template);
        _shape.Bind(StrokeLineCapProperty, this.GetObservable(StrokeLineCapProperty), BindingPriority.Template);
        _shape.Bind(StrokeJoinProperty, this.GetObservable(StrokeJoinProperty), BindingPriority.Template);
        _renderer = new InnerRenderer();
        foreach (var property in InnerRenderer.PaintProperties)
            _renderer.Bind(property, this.GetObservable(property), BindingPriority.Template);
        Children.Add(_renderer);
        _renderer.OnRender += OnRender;
        _shape.OnCreateDefiningGeometry += OnCreateDefiningGeometry;
        _shape.GeometryChanged += OnShapeGeometryChanged;
    }

    private void InvalidateGeometry()
    {
        _hitGeometry = null;
        _shape?.InvalidateGeometry();
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void OnShapeGeometryChanged()
    {
        _hitGeometry = null;
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected Geometry? RenderedGeometry => _shape?.RenderedGeometry;

    internal Geometry? GetHitGeometry()
    {
        // Dash collections can change without replacing the property value.
        if (StrokeDashArray != null)
            _hitGeometry = null;
        if (_hitGeometry == null && RenderedGeometry is { } geometry)
        {
            var group = new GeometryGroup { FillRule = FillRule.NonZero };
            if (Fill != null)
                group.Children.Add(geometry);
            if (Stroke != null && StrokeThickness > 0)
            {
                var pen = new Pen(Stroke, StrokeThickness,
                    StrokeDashArray == null ? null : new DashStyle(StrokeDashArray, StrokeDashOffset),
                    StrokeLineCap, StrokeJoin);
                group.Children.Add(geometry.GetWidenedGeometry(pen));
            }
            _hitGeometry = group;
        }
        return _hitGeometry;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StrokeProperty || change.Property == FillProperty ||
            change.Property == StrokeThicknessProperty || change.Property == StrokeDashArrayProperty ||
            change.Property == StrokeDashOffsetProperty || change.Property == StrokeLineCapProperty ||
            change.Property == StrokeJoinProperty || change.Property == StretchProperty || change.Property == BoundsProperty)
            _hitGeometry = null;
        if (change.Property == StretchProperty || change.Property == StrokeThicknessProperty || change.Property == StrokeProperty)
        {
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    
    private Geometry? OnCreateDefiningGeometry() => CreateDefiningGeometry();

    private new void InvalidateVisual() => _renderer?.InvalidateVisual();
    
    private void OnRender(DrawingContext drawingContext) => Render(drawingContext);

    protected virtual Geometry? CreateDefiningGeometry() => null;

    protected new virtual void Render(DrawingContext drawingContext) => _shape?.Render(drawingContext);

    protected override Size MeasureOverride(Size availableSize)
    {
        _shape!.Measure(availableSize);
        _renderer!.Measure(availableSize);
        return _shape.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _shape!.Arrange(new Rect(finalSize));
        _renderer!.Arrange(new Rect(finalSize));
        return finalSize;
    }
    
    /// <summary>
    /// Gets or sets the <see cref="T:Avalonia.Media.IBrush" /> that specifies how the shape's interior is painted.
    /// </summary>
    public IBrush? Fill
    {
      get => this.GetValue<IBrush?>(FillProperty);
      set => this.SetValue<IBrush?>(FillProperty, value);
    }

    /// <summary>
    /// Gets or sets a <see cref="P:Avalonia.Controls.Shapes.Shape.Stretch" /> enumeration value that describes how the shape fills its allocated space.
    /// </summary>
    public Stretch Stretch
    {
      get => this.GetValue<Stretch>(StretchProperty);
      set => this.SetValue<Stretch>(StretchProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="T:Avalonia.Media.IBrush" /> that specifies how the shape's outline is painted.
    /// </summary>
    public IBrush? Stroke
    {
      get => this.GetValue<IBrush?>(StrokeProperty);
      set => this.SetValue<IBrush?>(StrokeProperty, value);
    }

    /// <summary>
    /// Gets or sets a collection of <see cref="T:System.Double" /> values that indicate the pattern of dashes and gaps that is used to outline shapes.
    /// </summary>
    public AvaloniaList<double>? StrokeDashArray
    {
      get => this.GetValue<AvaloniaList<double>?>(StrokeDashArrayProperty);
      set => this.SetValue<AvaloniaList<double>?>(StrokeDashArrayProperty, value);
    }

    /// <summary>
    /// Gets or sets a value that specifies the distance within the dash pattern where a dash begins.
    /// </summary>
    public double StrokeDashOffset
    {
      get => this.GetValue<double>(StrokeDashOffsetProperty);
      set => this.SetValue<double>(StrokeDashOffsetProperty, value);
    }

    /// <summary>Gets or sets the width of the shape outline.</summary>
    public double StrokeThickness
    {
      get => this.GetValue<double>(StrokeThicknessProperty);
      set => this.SetValue<double>(StrokeThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets a <see cref="T:Avalonia.Media.PenLineCap" /> enumeration value that describes the shape at the ends of a line.
    /// </summary>
    public PenLineCap StrokeLineCap
    {
      get => this.GetValue<PenLineCap>(StrokeLineCapProperty);
      set => this.SetValue<PenLineCap>(StrokeLineCapProperty, value);
    }

    /// <summary>
    /// Gets or sets a <see cref="T:Avalonia.Media.PenLineJoin" /> enumeration value that specifies the type of join that is used at the vertices of a Shape.
    /// </summary>
    public PenLineJoin StrokeJoin
    {
      get => this.GetValue<PenLineJoin>(StrokeJoinProperty);
      set => this.SetValue<PenLineJoin>(StrokeJoinProperty, value);
    }
    
    private class InnerShape : Shape
    {
        public event Func<Geometry?>? OnCreateDefiningGeometry;
        public event Action? GeometryChanged;

        protected override void OnGeometryChanged(object? sender, EventArgs e)
        {
            base.OnGeometryChanged(sender, e);
            GeometryChanged?.Invoke();
        }
    
        protected override Geometry? CreateDefiningGeometry()
        {
            return OnCreateDefiningGeometry?.Invoke();
        }

        public new void InvalidateGeometry()
        {
            base.InvalidateGeometry();
        }
    }

    private class InnerRenderer : Control
    {
        public static readonly AvaloniaProperty[] PaintProperties;

        static InnerRenderer()
        {
            PaintProperties = new AvaloniaProperty[]
            {
                FillProperty.AddOwner<InnerRenderer>(), StrokeProperty.AddOwner<InnerRenderer>(),
                StrokeThicknessProperty.AddOwner<InnerRenderer>(), StrokeDashArrayProperty.AddOwner<InnerRenderer>(),
                StrokeDashOffsetProperty.AddOwner<InnerRenderer>(), StrokeLineCapProperty.AddOwner<InnerRenderer>(),
                StrokeJoinProperty.AddOwner<InnerRenderer>()
            };
            Visual.AffectsRender<InnerRenderer>(PaintProperties);
        }

        public event Action<DrawingContext>? OnRender;
    
        public override void Render(DrawingContext context)
        {
            base.Render(context);
            OnRender?.Invoke(context);
        }
    }
}
