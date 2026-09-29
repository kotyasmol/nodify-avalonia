# Connection interactions on Avalonia

Hold **Alt+Shift** and drag with the left mouse button to cut connections.
Escape, loss of pointer capture, or detaching the editor cancels the gesture.
Set `NodifyEditor.EnableCuttingLinePreview` to highlight crossed connections.

Intersection uses the rendered path and stroke, including dash patterns. The cut
has a width of one graph unit. The implementation uses the rectangle/path
intersection available in Avalonia 11.1 and does not need a framework upgrade.

Custom connection controls can register their type in
`NodifyEditor.CuttingConnectionTypes`. `Shape` and `BaseConnection` descendants
use their geometry; other registered controls use their bounds.

Connections show an outline on hover and support Avalonia context menus.
`DisplayConnectionsOnTop` changes their layer relative to nodes. The outline,
stroke and label render in that order so the outline does not cover the line.
