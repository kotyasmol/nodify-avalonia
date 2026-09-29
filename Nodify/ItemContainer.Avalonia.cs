using Avalonia;
using Avalonia.Controls;

namespace Nodify
{
    public partial class ItemContainer
    {
        private BitmapCache? _bitmapCache;
        private IDisposable? _bitmapCacheValue;

        internal void UpdateBitmapCache()
        {
            if (!Editor.ShouldCacheNodes)
            {
                ClearBitmapCache();
                return;
            }
            double scale = Editor.ViewportZoom <= 0.25 ? 0.25 : Editor.ViewportZoom <= 0.5 ? 0.5 : 1;
            double dpi = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
            double pixels = Bounds.Width * Bounds.Height * scale * scale * dpi * dpi;
            var viewport = new Rect(Editor.ViewportLocation, Editor.ViewportSize).Inflate(64 / Editor.ViewportZoom);
            if (pixels > 0 && pixels <= Editor.NodeCacheMaxPixels &&
                viewport.Intersects(new Rect(Location, Bounds.Size)))
            {
                if (_bitmapCache == null)
                {
                    _bitmapCache = new BitmapCache { RenderAtScale = scale };
                    _bitmapCacheValue = SetValue(CacheModeProperty, _bitmapCache, BindingPriority.Style);
                    Editor.TrackNodeCache(this, true);
                }
                else
                    _bitmapCache.RenderAtScale = scale;
            }
            else
                ClearBitmapCache();
        }

        private void ClearBitmapCache()
        {
            if (_bitmapCache != null)
                Editor.TrackNodeCache(this, false);
            _bitmapCacheValue?.Dispose();
            _bitmapCacheValue = null;
            _bitmapCache = null;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Editor.RegisterContainer(this);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            Editor.UnregisterContainer(this);
            ClearBitmapCache();
            base.OnDetachedFromVisualTree(e);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == BoundsProperty)
                UpdateBitmapCache();
            if (change.Property == IsPreviewingSelectionProperty)
            {
                UpdatePseudoClasses();
            }
        }

        private void UpdatePseudoClasses()
        {
            PseudoClasses.Set(":previewing-selection", IsPreviewingSelection == true);
            PseudoClasses.Set(":not-previewing-selection", IsPreviewingSelection == false);
            PseudoClasses.Set(":null-previewing-selection", IsPreviewingSelection == null);
        }
    }
}
