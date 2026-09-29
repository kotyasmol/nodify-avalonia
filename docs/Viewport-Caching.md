# Viewport caching (experimental)

These options require Avalonia 12. They are available in Playground's editor settings.

```xml
<nodify:NodifyEditor EnableNodeCaching="True"
                     NodeCacheMaxPixels="1048576"
                     EnableConnectionCulling="True" />
```

## Node caches

`EnableNodeCaching` caches visible nodes independently of the automatic zoom threshold.
Its default is `false`: the existing automatic policy still applies when
`EnableRenderingContainersOptimizations` is enabled, the graph has at least
`OptimizeRenderingMinimumContainers` nodes (default 700), and
`ViewportZoom / (1 - MinViewportZoom)` is at most `OptimizeRenderingZoomOutPercent`
(default 0.3). Disable both switches to turn off automatic node caches.

Caches are limited to nodes within the viewport plus a 64 DIP screen margin.
They use quarter, half or full resolution at zoom levels of at most 0.25, at most
0.5, or above 0.5 respectively. Device scaling is included in the pixel budget.
`NodeCacheMaxPixels` is a limit **per node**, not a total GPU memory budget; nodes
that exceed it render normally. Nodes release automatic caches when detached or
outside the margin. An application-set local `CacheMode` takes precedence.

## Offscreen connections

`EnableConnectionCulling` defaults to `false`. When enabled, it hides the internal
renderer and pauses directional-arrow animations outside the viewport margin.
The bounds include the connection path, its outline and label, so a connection
crossing the screen remains visible even when both endpoints are offscreen.
Changes to the viewport, geometry or label schedule a check before rendering.
Stretched connections and connections with a render transform skip culling.

Culling does not change the connection's `IsVisible` value or remove its control.
Controls, bindings and layout still exist; this is not node virtualization.

## Validation and remaining work

Headless tests cover viewport changes, crossing connections, visible labels,
animation restoration, per-node budgets and application-owned caches. Before
enabling these options in a release, measure interactive frame times and GPU
memory with representative graphs on the supported rendering backends, including
high-DPI displays and animated node content. The connection redraw benchmark in
this repository measures a separate geometry/text optimization and does not
establish a performance gain for these options.
