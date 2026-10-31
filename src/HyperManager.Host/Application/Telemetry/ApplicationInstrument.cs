namespace HyperManager.Host.Application.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public sealed class ApplicationInstrument : IDisposable
{
    private readonly Meter meter;

    private readonly MonitorService monitorService;

    public ActivitySource ActivitySource { get; }

    private readonly Counter<long> requestLongExecution;

    private readonly Counter<long> requestExecution;

    public ApplicationInstrument(IMeterFactory meterFactory, MonitorService monitorService)
    {
        this.monitorService = monitorService;

        ActivitySource = new ActivitySource(Source.Name, Source.Version);
        meter = meterFactory.Create(Source.Name, Source.Version);

        meter.CreateObservableCounter("application.uptime", ObserveApplicationUptime);

        requestLongExecution = meter.CreateCounter<long>("api.request.long.execution", description: "Long execution count.");
        requestExecution = meter.CreateCounter<long>("api.request.execution", description: "API request count");

        // Latest snapshot of the monitored hosts
        meter.CreateObservableGauge("monitor.host.count", ObserveHostCount, description: "Monitored host count by reachability.");
        meter.CreateObservableGauge("monitor.vm.count", ObserveVmCount, description: "Virtual machine count by host and state.");
    }

    public void Dispose()
    {
        meter.Dispose();
    }

    private static long ObserveApplicationUptime() =>
        (long)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds;

    private IEnumerable<Measurement<int>> ObserveHostCount()
    {
        var hosts = monitorService.Current.Hosts;
        var reachable = hosts.Count(static x => x.IsReachable);
        return
        [
            new(reachable, new KeyValuePair<string, object?>("status", "reachable")),
            new(hosts.Count - reachable, new KeyValuePair<string, object?>("status", "unreachable"))
        ];
    }

    private IEnumerable<Measurement<int>> ObserveVmCount() =>
        monitorService.Current.Hosts
            .SelectMany(static x => x.Vms.Select(vm => (Host: x.Name, vm.State)))
            .GroupBy(static x => x)
            .Select(static x => new Measurement<int>(x.Count(), new("host", x.Key.Host), new("state", x.Key.State.ToString())));

    public void IncrementRequestExecution(string method, string route) =>
        requestExecution.Add(1, new("http.request.method", method), new("http.route", route));

    public void IncrementRequestLongExecution(string method, string route) =>
        requestLongExecution.Add(1, new("http.request.method", method), new("http.route", route));
}
