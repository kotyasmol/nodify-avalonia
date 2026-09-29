using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;

namespace Nodify.Tests;

internal sealed class EditorTestView : IDisposable
{
    public TestEditor Editor { get; } = new();
    public Window Window { get; }

    public EditorTestView(params BaseConnection[] connections)
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
