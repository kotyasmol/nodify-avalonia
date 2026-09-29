using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Nodify.Tests;

public class ConnectionInteractionTests
{
    [AvaloniaFact]
    public void OutlineIsDrawnBehindTheStroke()
    {
        var connection = new ConnectionProbe
        {
            Source = new Point(100, 100), Target = new Point(400, 100), Spacing = 0,
            OutlineBrush = Brushes.Red, Stroke = Brushes.Blue
        };
        using var view = new EditorTestView(connection);
        var drawings = connection.Draw().Children.OfType<GeometryDrawing>().ToArray();
        Assert.Equal(2, drawings.Length);
        Assert.Equal(Brushes.Red, drawings[0].Pen!.Brush);
        Assert.Equal(Brushes.Blue, drawings[1].Pen!.Brush);
    }

    [AvaloniaFact]
    public void ConnectionGalleryRenders()
    {
        BaseConnection[] connections =
        [
            new Connection(), new LineConnection(), new CircuitConnection(), new StepConnection()
        ];
        for (int i = 0; i < connections.Length; i++)
        {
            connections[i].Source = new Point(70, 70 + i * 120);
            connections[i].Target = new Point(650, 120 + i * 120);
            connections[i].Text = connections[i].GetType().Name;
            connections[i].OutlineBrush = Brushes.DarkBlue;
            connections[i].DirectionalArrowsCount = 2;
        }
        using var view = new EditorTestView(connections);
        using var bitmap = view.Window.CaptureRenderedFrame();
        Assert.NotNull(bitmap);
        if (Environment.GetEnvironmentVariable("NODIFY_TEST_SCREENSHOTS") is { } directory)
        {
            Directory.CreateDirectory(directory);
            bitmap.Save(Path.Combine(directory, "connections.png"));
        }
    }

    [AvaloniaFact]
    public void ConnectionLayerFollowsDisplayConnectionsOnTop()
    {
        using var view = new EditorTestView();
        var host = view.Editor.GetVisualDescendants().OfType<ItemsControl>().Single(x => x.Name == "PART_ConnectionsHost");
        Assert.Equal(-1, host.ZIndex);
        view.Editor.DisplayConnectionsOnTop = true;
        Assert.Equal(0, host.ZIndex);
        view.Editor.DisplayConnectionsOnTop = false;
        Assert.Equal(-1, host.ZIndex);
    }

    [AvaloniaFact]
    public void ConnectionReceivesHoverAndContextMenu()
    {
        var menu = new ContextMenu { ItemsSource = new[] { new MenuItem { Header = "Delete" } } };
        var connection = new LineConnection { Source = new Point(100, 100), Target = new Point(400, 100), Spacing = 0, ContextMenu = menu };
        using var view = new EditorTestView(connection);
        view.Window.MouseMove(new Point(250, 100));
        view.Flush();
        Assert.True(connection.IsPointerOver);
        Assert.NotNull(connection.OutlineBrush);
        view.Window.MouseDown(new Point(250, 100), MouseButton.Right);
        view.Window.MouseUp(new Point(250, 100), MouseButton.Right);
        view.Flush();
        Assert.True(menu.IsOpen);
        menu.Close();
    }

    private sealed class ConnectionProbe : LineConnection
    {
        protected override Type StyleKeyOverride => typeof(LineConnection);

        public DrawingGroup Draw()
        {
            var drawing = new DrawingGroup();
            using (var context = drawing.Open())
                Render(context);
            return drawing;
        }
    }
}
