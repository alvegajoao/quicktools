namespace QuickTools.Services;

public sealed class AutoClickerService
{
    private readonly MouseInputService _mouseInputService;
    private CancellationTokenSource? _cancellationTokenSource;

    public AutoClickerService(MouseInputService mouseInputService)
    {
        _mouseInputService = mouseInputService;
    }

    public bool IsRunning => _cancellationTokenSource is { IsCancellationRequested: false };

    public event EventHandler? StateChanged;

    public void Start(int intervalMilliseconds, string button, string clickType)
    {
        if (IsRunning)
        {
            return;
        }

        var interval = Math.Max(10, intervalMilliseconds);
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        StateChanged?.Invoke(this, EventArgs.Empty);

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                _mouseInputService.Click(button, clickType);
                try
                {
                    await Task.Delay(interval, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Toggle(int intervalMilliseconds, string button, string clickType)
    {
        if (IsRunning)
        {
            Stop();
        }
        else
        {
            Start(intervalMilliseconds, button, clickType);
        }
    }
}
