# Rendering large graphs in Avalonia

The Avalonia port targets Avalonia 12.0.3 and .NET 8 or later. Desktop examples
target .NET 9; the browser example requires the .NET 10 SDK and its `wasm-tools`
workload.

## Node caches

Set `EnableNodeCaching="True"` on the editor to cache visible nodes as bitmaps.
This is useful for nodes with expensive templates whose content changes less
often than their position. Animated or frequently edited content may benefit
less; compare both settings with your own templates.

The existing automatic settings also work: `EnableRenderingContainersOptimizations`,
`OptimizeRenderingMinimumContainers`, and `OptimizeRenderingZoomOutPercent` enable
caching when a sufficiently large graph is zoomed out. Caches are per node, so
the empty space between distant nodes does not become a large bitmap.

`NodeCacheMaxPixels` limits each cache to 1,048,576 device pixels by default.
Larger nodes are drawn normally. Caches outside the viewport and its 64-DIP
margin are released. Rasterization uses three resolution levels (0.25, 0.5, 1)
to avoid rebuilding textures for every wheel delta. Local `CacheMode` settings
on an item container take precedence over the editor's cache.

## Connections outside the viewport

Set `EnableConnectionCulling="True"` to stop drawing offscreen connections and
pause their directional-arrow animations. Visibility uses the path's bounds,
including arrowheads, outline and label, so connections crossing the viewport
remain visible even when both endpoints are offscreen.

This option preserves controls, bindings, selection, and connector anchors.
It is not container virtualization and does not reduce the memory needed for
the graph's view models and controls. Viewport changes inspect all registered
connections once per scheduled update; endpoint changes inspect only dirty
connections. Connections with a stretch mode or a render transform use normal
rendering.

## Cutting connections

Hold **Alt+Shift** and drag with the left mouse button to cut connections.
Escape or loss of pointer capture cancels the gesture. Set
`NodifyEditor.EnableCuttingLinePreview` to highlight intersected connections.

Cutting uses the rendered path and stroke, rather than just its bounding box.
It works on Avalonia 12.0.3 using rectangle/path intersection; it does not depend
on the newer geometry hit-testing API. The cut has a one-unit width in graph
coordinates. Custom connection controls can register their type in
`NodifyEditor.CuttingConnectionTypes`; `Shape` and `BaseConnection` descendants
use their geometry, while other controls use their bounds.

## Measuring changes

Run the rendering preparation benchmark in Release:

```sh
dotnet run -c Release --project Benchmarks/Nodify.RenderingBenchmark
```

It reports time, managed allocations, and path construction counts for repeated
redraws of 100 and 1,000 connections, with labels and outlines independently
enabled. It warms up each scene before measuring 100 redraws. It exercises the
connection drawing commands (path, outline and label), not GPU rasterization or frame
presentation. The reported time must not be interpreted as application FPS.

For an application benchmark, use a fixed graph and node templates. Measure
idle, pan, zoom, single-node drag and group drag separately. Include long edges,
many short edges, and a viewport showing only a small part of the graph. Record
frame-time percentiles, UI-thread allocations and memory after repeatedly
opening and closing the editor. Keep the renderer, display scaling, window
size and Avalonia version constant when comparing results.

Run interaction and lifecycle tests with:

```sh
dotnet test Tests/Nodify.Tests/Nodify.Tests.csproj -c Release
```

These tests use Avalonia Headless with Skia, so no display server is required.

## Reference run

Measured on macOS arm64 with .NET 8.0.30, Avalonia 12.0.3 and Skia, in Release.
The baseline is `a8c9a96` (the Avalonia 12 update from PR #45), using the same
benchmark source. Each result is the median of three separate processes;
baseline and changed builds were run alternately. The table covers 1,000
connections and 100 redraws after warmup. Allocation totals include the drawing
commands recorded by the benchmark.

| Labels | Outline | Before (ms) | After (ms) | Before (MB allocated) | After (MB allocated) |
| --- | --- | ---: | ---: | ---: | ---: |
| No | No | 44.36 | 42.89 | 99.33 | 99.33 |
| No | Yes | 288.19 | 95.84 | 267.37 | 198.57 |
| Yes | No | 2712.23 | 1743.81 | 817.59 | 547.29 |
| Yes | Yes | 3102.69 | 1773.98 | 987.27 | 648.18 |

With outlines enabled, geometry construction during the measured redraws drops
from 100,000 calls to zero. Endpoint and shape changes still invalidate the path.
These measurements isolate geometry/text reuse; node caching and viewport culling
need application-level measurements with representative node templates.

The [raw measurements](../Benchmarks/results/rendering-macos-arm64.csv) include
all runs and both graph sizes. Timings depend on the machine and runtime; use
them as a comparison for this workload, not as a general rendering guarantee.
