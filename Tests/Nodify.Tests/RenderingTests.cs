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

public class RenderingTests
{
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
        using var view = new EditorView(connections);
        using var bitmap = view.Window.CaptureRenderedFrame();
        Assert.NotNull(bitmap);
        if (Environment.GetEnvironmentVariable("NODIFY_TEST_SCREENSHOTS") is { } directory)
        {
            Directory.CreateDirectory(directory);
            bitmap.Save(Path.Combine(directory, "connections.png"));
        }
    }

    [AvaloniaFact]
    public void OutlineAndLineShareGeometry()
    {
        var connection = new CountingConnection
        {
            Source = new Point(20, 30), Target = new Point(300, 140), OutlineBrush = Brushes.Red
        };
        using var view = new EditorView(connection);
        int before = connection.GeometryBuilds;
        connection.DrawConnection();
        connection.DrawConnection();
        Assert.Equal(before, connection.GeometryBuilds);

        connection.Target = new Point(320, 180);
        view.Flush();
        Assert.Equal(before + 1, connection.GeometryBuilds);
        connection.DrawConnection();
        Assert.Equal(before + 1, connection.GeometryBuilds);
    }

    [AvaloniaFact]
    public void MovingConnectionReusesTextButFontAndTextChangesRefreshIt()
    {
        var connection = new CountingConnection { Text = "flow", Source = new Point(20, 30), Target = new Point(300, 140) };
        using var view = new EditorView(connection);
        connection.DrawConnection();
        var original = connection.LastText;
        connection.Target = new Point(350, 180);
        view.Flush();
        connection.DrawConnection();
        Assert.Same(original, connection.LastText);
        connection.Text = "a longer label";
        connection.DrawConnection();
        Assert.NotSame(original, connection.LastText);
        Assert.True(connection.LastText!.Width > original!.Width);
        var updated = connection.LastText;
        connection.FontSize *= 2;
        connection.DrawConnection();
        Assert.True(connection.LastText!.Height > updated!.Height);
    }

    [AvaloniaFact]
    public void OutlineTracksStrokeThicknessWithoutRebuildingPath()
    {
        var connection = new CountingConnection { Source = new Point(20, 30), Target = new Point(300, 140), OutlineBrush = Brushes.Red };
        using var view = new EditorView(connection);
        int before = connection.GeometryBuilds;
        var initial = connection.DrawConnection().Children.OfType<GeometryDrawing>().ToArray();
        Assert.Equal(2, initial.Length);
        Assert.Equal(Brushes.Red, initial[0].Pen!.Brush);
        Assert.Equal(connection.Stroke, initial[1].Pen!.Brush);
        var thin = initial[0].Pen!.Thickness;
        connection.StrokeThickness = 12;
        var thick = connection.DrawConnection().Children.OfType<GeometryDrawing>().First().Pen!.Thickness;
        Assert.True(thick > thin);
        Assert.Equal(before, connection.GeometryBuilds);
    }

    [AvaloniaFact]
    public void CircuitAngleInvalidatesPath()
    {
        var connection = new TestCircuit { Source = new Point(20, 30), Target = new Point(300, 140) };
        using var view = new EditorView(connection);
        var original = connection.Path;
        connection.Angle = 30;
        Assert.NotSame(original, connection.Path);
    }

    [AvaloniaFact]
    public void ConnectionLayerFollowsDisplayConnectionsOnTop()
    {
        using var view = new EditorView();
        var host = view.Editor.GetVisualDescendants().OfType<ItemsControl>().Single(x => x.Name == "PART_ConnectionsHost");
        Assert.Equal(-1, host.ZIndex);
        view.Editor.DisplayConnectionsOnTop = true;
        Assert.Equal(0, host.ZIndex);
        view.Editor.DisplayConnectionsOnTop = false;
        Assert.Equal(-1, host.ZIndex);
    }

    [AvaloniaFact]
    public void CullingPreservesCrossingEdgesAndRestoresMovedEdges()
    {
        var outside = new LineConnection { Source = new Point(1500, 100), Target = new Point(1700, 200) };
        var crossing = new LineConnection { Source = new Point(-500, 200), Target = new Point(1500, 200), Spacing = 0 };
        using var view = new EditorView(outside, crossing);
        view.Editor.EnableConnectionCulling = true;
        view.Flush();
        Assert.All(outside.GetVisualChildren().OfType<Control>(), x => Assert.False(x.IsVisible));
        Assert.All(crossing.GetVisualChildren().OfType<Control>(), x => Assert.True(x.IsVisible));
        outside.Source = new Point(100, 100);
        outside.Target = new Point(200, 200);
        view.Flush();
        Assert.All(outside.GetVisualChildren().OfType<Control>(), x => Assert.True(x.IsVisible));
        view.Editor.ViewportLocation = new Point(2000, 2000);
        view.Flush();
        Assert.All(outside.GetVisualChildren().OfType<Control>(), x => Assert.False(x.IsVisible));
        view.Editor.EnableConnectionCulling = false;
        view.Flush();
        Assert.All(outside.GetVisualChildren().OfType<Control>(), x => Assert.True(x.IsVisible));
    }

    [AvaloniaFact]
    public void CullingKeepsVisibleLabelsAndDoesNotOverrideUserVisibility()
    {
        var connection = new LineConnection
        {
            Source = new Point(1000, 200), Target = new Point(1200, 200), Text = new string('M', 100), Spacing = 0
        };
        using var view = new EditorView(connection);
        view.Editor.EnableConnectionCulling = true;
        view.Flush();
        Assert.All(connection.GetVisualChildren().OfType<Control>(), x => Assert.True(x.IsVisible));
        connection.IsVisible = false;
        view.Editor.ViewportLocation = new Point(5000, 0);
        view.Flush();
        view.Editor.EnableConnectionCulling = false;
        view.Flush();
        Assert.False(connection.IsVisible);
    }

    [AvaloniaFact]
    public void NodeCachingHonorsPixelBudgetAndUserCache()
    {
        using var view = new EditorView();
        var item = new ItemContainer(view.Editor) { Content = new Border { Width = 200, Height = 100 } };
        view.Editor.Items.Add(item);
        view.Editor.EnableNodeCaching = true;
        view.Flush();
        Assert.IsType<BitmapCache>(item.CacheMode);
        view.Editor.NodeCacheMaxPixels = 10;
        Assert.Null(item.CacheMode);
        var userCache = new BitmapCache();
        item.CacheMode = userCache;
        view.Editor.NodeCacheMaxPixels = 1000000;
        Assert.Same(userCache, item.CacheMode);
        view.Editor.EnableNodeCaching = false;
        Assert.Same(userCache, item.CacheMode);
    }

    [AvaloniaFact]
    public void ConnectionReceivesHoverAndContextMenu()
    {
        var menu = new ContextMenu { ItemsSource = new[] { new MenuItem { Header = "Delete" } } };
        var connection = new LineConnection { Source = new Point(100, 100), Target = new Point(400, 100), Spacing = 0, ContextMenu = menu };
        using var view = new EditorView(connection);
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

    private sealed class TestCircuit : CircuitConnection
    {
        protected override Type StyleKeyOverride => typeof(CircuitConnection);
        public Geometry? Path => DefiningGeometry;
    }

    private sealed class CountingConnection : Connection
    {
        protected override Type StyleKeyOverride => typeof(Connection);
        public int GeometryBuilds { get; private set; }
        public FormattedText? LastText { get; private set; }
        protected override Geometry CreateDefiningGeometry()
        {
            GeometryBuilds++;
            return base.CreateDefiningGeometry();
        }
        protected override Point GetTextPosition(FormattedText text, Point source, Point target)
        {
            LastText = text;
            return base.GetTextPosition(text, source, target);
        }
        public DrawingGroup DrawConnection()
        {
            var drawing = new DrawingGroup();
            using (var context = drawing.Open())
                Render(context);
            return drawing;
        }
    }
}

internal sealed class EditorView : IDisposable
{
    public TestEditor Editor { get; } = new();
    public Window Window { get; }

    public EditorView(params BaseConnection[] connections)
    {
        Editor.ConnectionTemplate = null!;
        Editor.Connections = connections;
        Window = new Window { Width = 800, Height = 600, Content = Editor };
        Window.Show();
        Flush();
    }

    public void Flush()
    {
        Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    public void Dispose()
    {
        Window.Close();
        Dispatcher.UIThread.RunJobs();
    }
}

internal sealed class TestEditor : NodifyEditor
{
    public void Cut(Point start, Point end)
    {
        StartCutting(start);
        EndCutting(end);
    }
}
