namespace Nodify;

public partial class NodifyEditor
{
    /// <summary>Enables bitmap caching of individual nodes, independently of the automatic zoom-out threshold.</summary>
    public static readonly StyledProperty<bool> EnableNodeCachingProperty =
        AvaloniaProperty.Register<NodifyEditor, bool>(nameof(EnableNodeCaching));

    /// <summary>Maximum number of device pixels in an automatically created node cache.</summary>
    public static readonly StyledProperty<uint> NodeCacheMaxPixelsProperty =
        AvaloniaProperty.Register<NodifyEditor, uint>(nameof(NodeCacheMaxPixels), 1024 * 1024);

    /// <summary>Skips drawing and directional-arrow animations for connections outside the viewport.</summary>
    public static readonly StyledProperty<bool> EnableConnectionCullingProperty =
        AvaloniaProperty.Register<NodifyEditor, bool>(nameof(EnableConnectionCulling));

    /// <summary>Gets or sets whether to cache visible nodes independently of the automatic zoom-out threshold.</summary>
    public bool EnableNodeCaching
    {
        get => GetValue(EnableNodeCachingProperty);
        set => SetValue(EnableNodeCachingProperty, value);
    }

    /// <summary>Gets or sets the maximum number of device pixels in each automatically created node cache.</summary>
    public uint NodeCacheMaxPixels
    {
        get => GetValue(NodeCacheMaxPixelsProperty);
        set => SetValue(NodeCacheMaxPixelsProperty, value);
    }

    /// <summary>Gets or sets whether to skip offscreen connection drawing and arrow animations.</summary>
    public bool EnableConnectionCulling
    {
        get => GetValue(EnableConnectionCullingProperty);
        set => SetValue(EnableConnectionCullingProperty, value);
    }

    private readonly HashSet<ItemContainer> _renderingContainers = new();
    private readonly HashSet<ItemContainer> _cachedContainers = new();
    private readonly HashSet<BaseConnection> _renderingConnections = new();
    private readonly HashSet<BaseConnection> _dirtyConnections = new();
    private bool _connectionViewportDirty;
    private bool _nodeCachesDirty;
    private bool _renderingUpdatePending;

    internal bool ShouldCacheNodes => EnableNodeCaching ||
        (EnableRenderingContainersOptimizations && Items.Count >= OptimizeRenderingMinimumContainers &&
         ViewportZoom / Math.Max(0.001, 1 - MinViewportZoom) <= OptimizeRenderingZoomOutPercent);

    internal void TrackNodeCache(ItemContainer container, bool cached)
    {
        if (cached)
            _cachedContainers.Add(container);
        else
            _cachedContainers.Remove(container);
    }

    internal void RegisterContainer(ItemContainer container)
    {
        _renderingContainers.Add(container);
        _nodeCachesDirty = true;
        ScheduleRenderingUpdate();
    }

    internal void UnregisterContainer(ItemContainer container)
    {
        _renderingContainers.Remove(container);
        _nodeCachesDirty = true;
        ScheduleRenderingUpdate();
    }

    internal void RegisterConnection(BaseConnection connection)
    {
        _renderingConnections.Add(connection);
        InvalidateConnection(connection);
    }

    internal void UnregisterConnection(BaseConnection connection)
    {
        _renderingConnections.Remove(connection);
        _dirtyConnections.Remove(connection);
    }

    internal void InvalidateConnection(BaseConnection connection)
    {
        if (EnableConnectionCulling)
        {
            _dirtyConnections.Add(connection);
            ScheduleRenderingUpdate();
        }
    }

    private void InvalidateConnectionViewport(bool force = false)
    {
        _nodeCachesDirty = true;
        _connectionViewportDirty |= EnableConnectionCulling || force;
        ScheduleRenderingUpdate();
    }

    private void ScheduleRenderingUpdate()
    {
        if (!_renderingUpdatePending)
        {
            _renderingUpdatePending = true;
            Dispatcher.UIThread.Post(UpdateRendering, DispatcherPriority.Render);
        }
    }

    private void UpdateRendering()
    {
        _renderingUpdatePending = false;
        if (_nodeCachesDirty)
        {
            _nodeCachesDirty = false;
            ApplyRenderingOptimizations();
        }

        var connections = _connectionViewportDirty ? _renderingConnections.ToArray() : _dirtyConnections.ToArray();
        _connectionViewportDirty = false;
        _dirtyConnections.Clear();
        // The margin is in screen DIPs, so zooming does not shrink the prefetch area.
        var viewport = new Rect(ViewportLocation, ViewportSize).Inflate(64 / ViewportZoom);
        foreach (var connection in connections)
        {
            bool culled = false;
            if (EnableConnectionCulling && ViewportSize.Width > 0 && ViewportSize.Height > 0 &&
                connection.Stretch == Stretch.None && connection.RenderTransform == null)
            {
                var bounds = connection.GetConnectionBounds();
                var transform = connection.TransformToVisual(ItemsHost);
                if (transform.HasValue)
                    culled = !viewport.Intersects(bounds.TransformToAABB(transform.Value));
            }
            connection.SetCulled(culled);
        }
    }
}
