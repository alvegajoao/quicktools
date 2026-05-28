using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using QuickTools.Models;

namespace QuickTools.Services;

public sealed class PowerService
{
    private const string BalancedGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";
    private const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private const string PowerSaverGuid = "a1841308-3541-4fab-bc81-f71556f20b4a";
    private const string UltimatePerformanceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";

    private CancellationTokenSource? _scheduledToken;
    private ScheduledPowerAction? _scheduledAction;
    private readonly Dictionary<string, (ScheduledEvent Event, CancellationTokenSource Cts)> _events = new();

    public ScheduledPowerAction? ScheduledAction => _scheduledAction;
    public event EventHandler? ScheduledActionChanged;
    public event EventHandler? EventsChanged;

    public IReadOnlyList<ScheduledEvent> ScheduledEvents =>
        _events.Values.Select(value => value.Event).OrderBy(item => item.ExecuteAt).ToList();

    public ScheduledEvent AddEvent(string action, DateTime executeAt)
    {
        var scheduledEvent = new ScheduledEvent { Action = action, ExecuteAt = executeAt };
        var cts = new CancellationTokenSource();
        _events[scheduledEvent.Id] = (scheduledEvent, cts);
        _ = RunEventAsync(scheduledEvent, cts.Token);
        EventsChanged?.Invoke(this, EventArgs.Empty);
        return scheduledEvent;
    }

    public void RemoveEvent(string id)
    {
        if (!_events.TryGetValue(id, out var entry))
        {
            return;
        }

        entry.Cts.Cancel();
        entry.Cts.Dispose();
        _events.Remove(id);
        EventsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void PauseEvent(string id)
    {
        if (!_events.TryGetValue(id, out var entry))
        {
            return;
        }

        entry.Cts.Cancel();
        entry.Cts.Dispose();
        entry.Event.IsActive = false;
        _events[id] = (entry.Event, new CancellationTokenSource());
        EventsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ResumeEvent(string id)
    {
        if (!_events.TryGetValue(id, out var entry) || entry.Event.ExecuteAt <= DateTime.Now)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        entry.Event.IsActive = true;
        _events[id] = (entry.Event, cts);
        _ = RunEventAsync(entry.Event, cts.Token);
        EventsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void PauseAllEvents()
    {
        var changed = false;

        foreach (var id in _events.Keys.ToList())
        {
            if (!_events.TryGetValue(id, out var entry) || !entry.Event.IsActive)
            {
                continue;
            }

            entry.Cts.Cancel();
            entry.Cts.Dispose();
            entry.Event.IsActive = false;
            _events[id] = (entry.Event, new CancellationTokenSource());
            changed = true;
        }

        if (changed)
        {
            EventsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void CancelAllEvents()
    {
        foreach (var (_, (_, cts)) in _events)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _events.Clear();
        _ = CancelScheduledActionAsync();
        EventsChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task RunEventAsync(ScheduledEvent scheduledEvent, CancellationToken token)
    {
        try
        {
            var delay = scheduledEvent.ExecuteAt - DateTime.Now;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, token);
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            await ExecuteEventActionAsync(scheduledEvent.Action);
            _events.Remove(scheduledEvent.Id);
            EventsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task ExecuteEventActionAsync(string action)
    {
        switch (action)
        {
            case "Shutdown":
                await RunProcessAsync("shutdown", "/s /t 0");
                break;
            case "Restart":
                await RunProcessAsync("shutdown", "/r /t 0");
                break;
            case "Hibernate":
                await RunProcessAsync("shutdown", "/h");
                break;
            case "Suspend":
                SetSuspendState(false, true, true);
                break;
        }
    }

    public async Task<IReadOnlyList<PowerPlan>> GetPowerPlansAsync()
    {
        var output = await RunProcessAsync("cmd.exe", "/d /c chcp 65001>nul & powercfg /L", Encoding.UTF8);
        var plans = new List<PowerPlan>();
        var regex = new Regex(@"([0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}).*?\((.*?)\)(.*)");

        foreach (var line in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var match = regex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            plans.Add(new PowerPlan
            {
                Guid = match.Groups[1].Value,
                Name = match.Groups[2].Value.Trim(),
                IsActive = match.Groups[3].Value.Contains('*')
            });
        }

        return plans;
    }

    public async Task SetPowerPlanAsync(string guid)
    {
        await RunProcessAsync("powercfg", $"/S {guid}");
    }

    public async Task SetPlanByKindAsync(string kind)
    {
        var plans = await GetPowerPlansAsync();
        var expectedGuid = kind switch
        {
            "Balanced" => BalancedGuid,
            "HighPerformance" => HighPerformanceGuid,
            "PowerSaver" => PowerSaverGuid,
            "UltimatePerformance" => UltimatePerformanceGuid,
            _ => ""
        };

        var plan = plans.FirstOrDefault(plan => plan.Guid.Equals(expectedGuid, StringComparison.OrdinalIgnoreCase));
        if (plan is null)
        {
            throw new InvalidOperationException($"Power plan '{kind}' is not available on this system.");
        }

        await SetPowerPlanAsync(plan.Guid);
    }

    public async Task ScheduleAsync(string action, DateTime executeAt)
    {
        await CancelScheduledActionAsync();

        var seconds = Math.Max(0, (int)Math.Ceiling((executeAt - DateTime.Now).TotalSeconds));
        _scheduledAction = new ScheduledPowerAction { Action = action, ExecuteAt = executeAt };
        ScheduledActionChanged?.Invoke(this, EventArgs.Empty);

        if (action == "Shutdown")
        {
            await RunProcessAsync("shutdown", $"/s /t {seconds}");
            return;
        }

        if (action == "Restart")
        {
            await RunProcessAsync("shutdown", $"/r /t {seconds}");
            return;
        }

        _scheduledToken = new CancellationTokenSource();
        _ = ExecuteDelayedSystemPowerActionAsync(action, TimeSpan.FromSeconds(seconds), _scheduledToken.Token);
    }

    public async Task CancelScheduledActionAsync()
    {
        _scheduledToken?.Cancel();
        _scheduledToken?.Dispose();
        _scheduledToken = null;
        _scheduledAction = null;

        try
        {
            await RunProcessAsync("shutdown", "/a");
        }
        catch
        {
            // shutdown /a returns an error when there is nothing to cancel.
        }

        ScheduledActionChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task ExecuteDelayedSystemPowerActionAsync(string action, TimeSpan delay, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (action == "Hibernate")
            {
                await RunProcessAsync("shutdown", "/h");
            }
            else if (action == "Suspend")
            {
                SetSuspendState(false, true, true);
            }
        }
        catch (TaskCanceledException)
        {
        }
        finally
        {
            _scheduledAction = null;
            ScheduledActionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static async Task<string> RunProcessAsync(string fileName, string arguments, Encoding? outputEncoding = null)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = outputEncoding,
            StandardErrorEncoding = outputEncoding
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Unable to start {fileName}.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException(error.Trim());
        }

        return output;
    }

    [System.Runtime.InteropServices.DllImport("powrprof.dll", SetLastError = true)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
}
