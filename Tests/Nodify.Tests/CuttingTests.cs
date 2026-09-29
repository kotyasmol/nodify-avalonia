using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Xunit;

namespace Nodify.Tests;

public class CuttingTests
{
    [AvaloniaFact]
    public void CutDisconnectsCrossedLineOnly()
    {
        var crossed = new LineConnection { Source = new Point(100, 100), Target = new Point(400, 100), Spacing = 0 };
        var missed = new LineConnection { Source = new Point(100, 300), Target = new Point(400, 300), Spacing = 0 };
        int removed = 0;
        crossed.Disconnect += (_, _) => removed++;
        missed.Disconnect += (_, _) => Assert.Fail("The cut does not intersect this edge.");
        using var view = new EditorView(crossed, missed);
        view.Editor.Cut(new Point(250, 50), new Point(250, 150));
        Assert.Equal(1, removed);
        Assert.False(view.Editor.IsCutting);
    }

    [AvaloniaFact]
    public void ClearingDashArrayRefreshesCutGeometry()
    {
        var connection = new LineConnection
        {
            Source = new Point(100, 100), Target = new Point(400, 100), Spacing = 0,
            SourceOffsetMode = ConnectionOffsetMode.None, TargetOffsetMode = ConnectionOffsetMode.None,
            ArrowEnds = ArrowHeadEnds.None, Fill = null, StrokeThickness = 2,
            StrokeDashArray = [5, 5]
        };
        int removed = 0;
        connection.Disconnect += (_, _) => removed++;
        using var view = new EditorView(connection);
        view.Editor.Cut(new Point(115, 50), new Point(115, 150));
        Assert.Equal(0, removed);

        connection.StrokeDashArray.Clear();
        view.Editor.Cut(new Point(115, 50), new Point(115, 150));
        Assert.Equal(1, removed);
    }

    [AvaloniaFact]
    public void CutUsesCurveGeometryInsteadOfBoundingBox()
    {
        var curve = new Connection { Source = new Point(100, 100), Target = new Point(400, 300), Spacing = 0 };
        int removed = 0;
        curve.Disconnect += (_, _) => removed++;
        using var view = new EditorView(curve);
        view.Editor.Cut(new Point(110, 270), new Point(130, 290));
        Assert.Equal(0, removed);
        view.Editor.Cut(new Point(250, 50), new Point(250, 350));
        Assert.Equal(1, removed);
    }

    [AvaloniaTheory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void CutWorksAfterZoomAndPan(double zoom)
    {
        var connection = new StepConnection { Source = new Point(100, 100), Target = new Point(400, 300) };
        int removed = 0;
        connection.Disconnect += (_, _) => removed++;
        using var view = new EditorView(connection);
        view.Editor.ViewportZoom = zoom;
        view.Editor.ViewportLocation = new Point(30, 40);
        view.Flush();
        view.Editor.Cut(new Point(50, 200), new Point(500, 200));
        Assert.Equal(1, removed);
    }

    [AvaloniaFact]
    public void ZeroLengthCutDoesNotDisconnect()
    {
        var connection = new LineConnection { Source = new Point(100, 100), Target = new Point(400, 100) };
        connection.Disconnect += (_, _) => Assert.Fail("Clicking without dragging must not cut a connection.");
        using var view = new EditorView(connection);
        view.Editor.Cut(new Point(250, 100), new Point(250, 100));
    }

    [AvaloniaTheory]
    [InlineData("escape")]
    [InlineData("capture")]
    [InlineData("detach")]
    public void CancelingCutClearsPreviewWithoutDisconnecting(string cancellation)
    {
        bool preview = NodifyEditor.EnableCuttingLinePreview;
        NodifyEditor.EnableCuttingLinePreview = true;
        try
        {
            var connection = new LineConnection { Source = new Point(100, 100), Target = new Point(400, 100), Spacing = 0 };
            connection.Disconnect += (_, _) => Assert.Fail("A canceled gesture must not disconnect.");
            using var view = new EditorView(connection);
            IPointer? pointer = null;
            view.Editor.PointerMoved += (_, e) => pointer = e.Pointer;
            view.Window.MouseDown(new Point(250, 50), MouseButton.Left, RawInputModifiers.Alt | RawInputModifiers.Shift);
            view.Window.MouseMove(new Point(250, 150), RawInputModifiers.Alt | RawInputModifiers.Shift | RawInputModifiers.LeftMouseButton);
            Assert.True(view.Editor.IsCutting);
            Assert.True(CuttingLine.GetIsOverElement(connection));

            if (cancellation == "capture")
            {
                Assert.NotNull(pointer);
                pointer.Capture(null);
            }
            else if (cancellation == "detach")
                view.Window.Content = null;
            else
                view.Window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);

            view.Flush();
            Assert.False(view.Editor.IsCutting);
            Assert.False(CuttingLine.GetIsOverElement(connection));
            view.Window.MouseUp(new Point(250, 150), MouseButton.Left);
        }
        finally
        {
            NodifyEditor.EnableCuttingLinePreview = preview;
        }
    }
}
