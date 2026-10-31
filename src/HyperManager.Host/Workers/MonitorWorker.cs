namespace HyperManager.Host.Workers;

// Refreshes the status of all hosts on a fixed interval
public sealed class MonitorWorker : BackgroundService
{
    private readonly ILogger<MonitorWorker> log;

    private readonly MonitorWorkerOption options;

    private readonly MonitorService monitorService;

    public MonitorWorker(
        ILogger<MonitorWorker> log,
        MonitorWorkerOption options,
        MonitorService monitorService)
    {
        this.log = log;
        this.options = options;
        this.monitorService = monitorService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enable)
        {
            log.InfoWorkerDisabled(nameof(MonitorWorker));
            return;
        }

        log.InfoWorkerStart(nameof(MonitorWorker));
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.IntervalSeconds));

            // Refresh once immediately so that the first opened page has data, then on each tick
            do
            {
                try
                {
                    await monitorService.RefreshAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.ErrorWorkerException(nameof(MonitorWorker), ex);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
        finally
        {
            log.InfoWorkerStop(nameof(MonitorWorker));
        }
    }
}
