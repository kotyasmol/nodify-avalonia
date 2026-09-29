using System.Threading;
using System.Globalization;

namespace Nodify;

public partial class BaseConnection
{
    private CancellationTokenSource? animationTokenSource;
    private double? _animationDuration;
    private bool _isAttached;
    private FormattedText? _formattedText;
    private CultureInfo? _textCulture;

    private FormattedText GetFormattedText()
    {
        var culture = CultureInfo.CurrentUICulture;
        if (_formattedText == null || !Equals(_textCulture, culture))
        {
            _textCulture = culture;
            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            _formattedText = new FormattedText(Text, culture, FlowDirection, typeface, FontSize, Foreground ?? Stroke);
        }
        return _formattedText;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty || change.Property == FontFamilyProperty ||
            change.Property == FontSizeProperty || change.Property == FontWeightProperty ||
            change.Property == FontStyleProperty || change.Property == FontStretchProperty ||
            change.Property == ForegroundProperty || change.Property == StrokeProperty ||
            change.Property == FlowDirectionProperty)
            _formattedText = null;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttached = true;
        UpdateAnimation();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttached = false;
        PauseAnimation();
        _container = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void UpdateAnimation()
    {
        PauseAnimation();
        if (_animationDuration is { } duration && _isAttached)
        {
            animationTokenSource = new();
            this.StartLoopingAnimation(DirectionalArrowsOffsetProperty, DirectionalArrowsOffset + 1d, duration, animationTokenSource.Token);
        }
    }

    private void PauseAnimation()
    {
        this.CancelAnimation(DirectionalArrowsOffsetProperty, animationTokenSource);
        animationTokenSource?.Dispose();
        animationTokenSource = null;
    }

    static BaseConnection()
    {
        AffectsRender<BaseConnection>(SourceProperty, TargetProperty, SourceOffsetProperty, TargetOffsetProperty,
            SourceOffsetModeProperty, TargetOffsetModeProperty, DirectionProperty, SpacingProperty, ArrowSizeProperty, ArrowEndsProperty, ArrowShapeProperty, TextProperty, DirectionalArrowsCountProperty, DirectionalArrowsOffsetProperty, OutlineThicknessProperty, OutlineBrushProperty,
            IsAnimatingDirectionalArrowsProperty, DirectionalArrowsAnimationDurationProperty);
        AffectsRender<BaseConnection>(FontFamilyProperty, FontSizeProperty, FontWeightProperty, FontStyleProperty,
            FontStretchProperty, ForegroundProperty, FlowDirectionProperty, StrokeProperty, StrokeThicknessProperty);
        AffectsGeometry<BaseConnection>(SourceProperty, TargetProperty, SourceOffsetProperty, TargetOffsetProperty,
            SourceOffsetModeProperty, TargetOffsetModeProperty, DirectionProperty, SpacingProperty, ArrowSizeProperty, ArrowEndsProperty, ArrowShapeProperty, SourceOrientationProperty, TargetOrientationProperty, DirectionalArrowsCountProperty, DirectionalArrowsOffsetProperty);
        OutlineBrushProperty.Changed.AddClassHandler<BaseConnection>(OnOutlinePenChanged);
        OutlineThicknessProperty.Changed.AddClassHandler<BaseConnection>(OnOutlinePenChanged);
        StrokeThicknessProperty.Changed.AddClassHandler<BaseConnection>(OnOutlinePenChanged);
        IsAnimatingDirectionalArrowsProperty.Changed.AddClassHandler<BaseConnection>(OnIsAnimatingDirectionalArrowsChanged);
        DirectionalArrowsAnimationDurationProperty.Changed.AddClassHandler<BaseConnection>(OnDirectionalArrowsAnimationDurationChanged);
        IsSelectedProperty.Changed.AddClassHandler<Control>(OnIsSelectedChanged);
    }
}
