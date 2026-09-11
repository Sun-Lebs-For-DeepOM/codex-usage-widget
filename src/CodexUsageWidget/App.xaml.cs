using System.Threading;
using System.Windows;

namespace CodexUsageWidget;

public partial class App : Application
{
    private const string MutexName = @"Local\OpenAI.CodexUsageWidget";
    private const string ActivationEventName = @"Local\OpenAI.CodexUsageWidget.Activate";
    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _activationEvent;
    private CancellationTokenSource? _activationWaitCancellation;
    private Task? _activationWaitTask;

    protected override void OnStartup(StartupEventArgs e)
    {
        _activationEvent = new EventWaitHandle(
            initialState: false,
            EventResetMode.AutoReset,
            ActivationEventName);
        _singleInstanceMutex = new Mutex(initiallyOwned: true, MutexName, out var isFirstInstance);

        if (!isFirstInstance)
        {
            _activationEvent.Set();
            Shutdown();
            return;
        }

        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"Codex 额度悬浮窗遇到错误：\n\n{args.Exception.Message}",
                "Codex 额度",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            args.Handled = true;
        };

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
        StartActivationListener(window);
    }

    private void StartActivationListener(MainWindow window)
    {
        var activationEvent = _activationEvent;
        if (activationEvent is null)
        {
            return;
        }

        _activationWaitCancellation = new CancellationTokenSource();
        var cancellationToken = _activationWaitCancellation.Token;
        _activationWaitTask = Task.Run(() =>
        {
            var waitHandles = new[] { activationEvent, cancellationToken.WaitHandle };
            while (true)
            {
                var signaledHandle = WaitHandle.WaitAny(waitHandles);
                if (signaledHandle == 1 || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                Dispatcher.BeginInvoke(window.RestoreVisibility);
            }
        }, cancellationToken);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _activationWaitCancellation?.Cancel();
        _activationEvent?.Set();

        try
        {
            _activationWaitTask?.Wait(TimeSpan.FromMilliseconds(500));
        }
        catch (AggregateException)
        {
            // Cancellation during shutdown is expected.
        }

        try
        {
            _singleInstanceMutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // The mutex may already have been released during abnormal shutdown.
        }

        _activationWaitCancellation?.Dispose();
        _activationEvent?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
