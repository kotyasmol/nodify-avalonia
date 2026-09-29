using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace Nodify.Tests;

public class ViewportCachingTests
{
    [AvaloniaFact]
    public void CullingPreservesCrossingEdgesAndRestoresMovedEdges()
    {
        var outside = new LineConnection { Source = new Point(1500, 100), Target = new Point(1700, 200) };
        var crossing = new LineConnection { Source = new Point(-500, 200), Target = new Point(1500, 200), Spacing = 0 };
        using var view = new EditorTestView(outside, crossing);
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
        using var view = new EditorTestView(connection);
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
        using var view = new EditorTestView();
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
    public void CullingPausesAndRestoresManuallyStartedAnimation()
    {
        var connection = new Connection
        {
            Source = new Point(1500, 100), Target = new Point(1700, 100), DirectionalArrowsCount = 3
        };
        using var view = new EditorTestView(connection);
        var field = typeof(BaseConnection).GetField("animationTokenSource", BindingFlags.Instance | BindingFlags.NonPublic)!;
        connection.StartAnimation();
        Assert.NotNull(field.GetValue(connection));
        view.Editor.EnableConnectionCulling = true;
        view.Flush();
        Assert.Null(field.GetValue(connection));
        view.Editor.ViewportLocation = new Point(1400, 0);
        view.Flush();
        Assert.NotNull(field.GetValue(connection));
        connection.StopAnimation();
        view.Editor.EnableConnectionCulling = false;
        view.Flush();
        Assert.Null(field.GetValue(connection));
    }
}
