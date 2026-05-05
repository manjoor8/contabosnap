using ContaboSnapshotService.Configuration;
using ContaboSnapshotService.Services;
using Microsoft.Extensions.Options;

namespace ContaboSnapshotService;

/// <summary>
/// Background worker that runs the snapshot rotation on a weekly schedule.
/// Calculates the next run time based on configured day-of-week and time,
/// then sleeps until that time before executing.
/// </summary>
public class Worker : BackgroundService
{
    private readonly SnapshotManagerService _snapshotManager;
    private readonly SnapshotScheduleSettings _schedule;
    private readonly ILogger<Worker> _logger;

    public Worker(
        SnapshotManagerService snapshotManager,
        IOptions<SnapshotScheduleSettings> schedule,
        ILogger<Worker> logger)
    {
        _snapshotManager = snapshotManager;
        _schedule = schedule.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Contabo Snapshot Worker started.");
        _logger.LogInformation("Schedule: Every {DayOfWeek} at {TimeOfDay}",
            _schedule.DayOfWeek, _schedule.TimeOfDay);

        // Run immediately on first start, then weekly
        bool isFirstRun = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (isFirstRun)
                {
                    _logger.LogInformation("Running initial snapshot rotation on startup...");
                    isFirstRun = false;
                }
                else
                {
                    var nextRun = CalculateNextRun();
                    var delay = nextRun - DateTime.Now;

                    if (delay > TimeSpan.Zero)
                    {
                        _logger.LogInformation("Next snapshot rotation scheduled for: {NextRun} (in {Hours}h {Minutes}m)",
                            nextRun, (int)delay.TotalHours, delay.Minutes);
                        await Task.Delay(delay, stoppingToken);
                    }
                }

                await _snapshotManager.RunSnapshotRotationAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker is shutting down gracefully.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during snapshot rotation. Will retry at next scheduled time.");

                // Wait 5 minutes before retrying on error to avoid tight error loops
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException) { break; }
            }
        }

        _logger.LogInformation("Contabo Snapshot Worker stopped.");
    }

    /// <summary>
    /// Calculates the next run time based on the configured schedule.
    /// </summary>
    private DateTime CalculateNextRun()
    {
        if (!Enum.TryParse<DayOfWeek>(_schedule.DayOfWeek, true, out var targetDay))
        {
            _logger.LogWarning("Invalid DayOfWeek '{Day}', defaulting to Sunday", _schedule.DayOfWeek);
            targetDay = DayOfWeek.Sunday;
        }

        if (!TimeSpan.TryParse(_schedule.TimeOfDay, out var targetTime))
        {
            _logger.LogWarning("Invalid TimeOfDay '{Time}', defaulting to 02:00", _schedule.TimeOfDay);
            targetTime = new TimeSpan(2, 0, 0);
        }

        var now = DateTime.Now;
        var today = now.Date;
        int daysUntilTarget = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;

        // If it's the target day but past the scheduled time, schedule for next week
        if (daysUntilTarget == 0 && now.TimeOfDay >= targetTime)
        {
            daysUntilTarget = 7;
        }

        // If daysUntilTarget is 0 (same day, before target time), use today
        if (daysUntilTarget == 0)
        {
            return today.Add(targetTime);
        }

        return today.AddDays(daysUntilTarget).Add(targetTime);
    }
}
