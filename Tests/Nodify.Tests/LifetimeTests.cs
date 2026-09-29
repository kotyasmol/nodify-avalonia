using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace Nodify.Tests;

public class LifetimeTests
{
    [AvaloniaFact]
    public void AutoPanningStopsOnDetachAndRestartsOnReattach()
    {
        using var view = new EditorTestView();
        var field = typeof(NodifyEditor).GetField("_autoPanningTimer", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var timer = Assert.IsType<DispatcherTimer>(field.GetValue(view.Editor));
        Assert.True(timer.IsEnabled);
        view.Window.Content = null;
        view.Flush();
        Assert.False(timer.IsEnabled);
        Assert.Null(field.GetValue(view.Editor));
        view.Window.Content = view.Editor;
        view.Flush();
        Assert.True(Assert.IsType<DispatcherTimer>(field.GetValue(view.Editor)).IsEnabled);
        view.Editor.DisableAutoPanning = true;
        Assert.Null(field.GetValue(view.Editor));
    }

    [AvaloniaFact]
    public void DetachedEditorCanBeCollected()
    {
        using var view = new EditorTestView();
        var selected = new ObservableCollection<object>();
        var selectedConnections = new ObservableCollection<object>();
        var reference = CreateAndRemoveEditor(view.Window, selected, selectedConnections);
        view.Flush();
        for (int i = 0; i < 3 && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Dispatcher.UIThread.RunJobs();
        }
        Assert.False(reference.IsAlive);
        GC.KeepAlive(selected);
        GC.KeepAlive(selectedConnections);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndRemoveEditor(Window window, ObservableCollection<object> selected,
        ObservableCollection<object> selectedConnections)
    {
        var editor = new NodifyEditor { SelectedItems = selected, SelectedConnections = selectedConnections };
        window.Content = editor;
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.Content = null;
        return new WeakReference(editor);
    }

    [AvaloniaFact]
    public void ArrowAnimationStopsOnDetachAndRestartsOnReattach()
    {
        var connection = new Connection
        {
            Source = new Point(100, 100), Target = new Point(400, 100),
            DirectionalArrowsCount = 3, IsAnimatingDirectionalArrows = true
        };
        using var view = new EditorTestView(connection);
        var field = typeof(BaseConnection).GetField("animationTokenSource", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.NotNull(field.GetValue(connection));
        view.Editor.Connections = Array.Empty<Connection>();
        view.Flush();
        Assert.Null(field.GetValue(connection));
        view.Editor.Connections = new[] { connection };
        view.Flush();
        Assert.NotNull(field.GetValue(connection));
    }
}
