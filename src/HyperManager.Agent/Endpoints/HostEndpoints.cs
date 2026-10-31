namespace HyperManager.Agent.Endpoints;

public static class HostEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapHostEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Prefix)
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/host", HandleGetHost)
            .WithName("HostGet")
            .Produces<HostInfo>();
        group.MapGet("/host/metrics", HandleGetMetricsAsync)
            .WithName("HostMetricsGet")
            .Produces<HostMetrics>()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        group.MapGet("/snapshot", HandleGetSnapshotAsync)
            .WithName("SnapshotGet")
            .Produces<HostSnapshot>()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    //--------------------------------------------------------------------------------
    // Host
    //--------------------------------------------------------------------------------

    private static Ok<HostInfo> HandleGetHost(IHyperVService hyperV) =>
        TypedResults.Ok(CreateHostInfo(hyperV));

    //--------------------------------------------------------------------------------
    // Metrics
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleGetMetricsAsync(
        IHyperVService hyperV,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await hyperV.GetHostMetricsAsync(cancellationToken));
        }
        catch (HyperVException ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    //--------------------------------------------------------------------------------
    // Snapshot
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleGetSnapshotAsync(
        IHyperVService hyperV,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // Query metrics and VMs concurrently
        var metricsTask = hyperV.GetHostMetricsAsync(cancellationToken).AsTask();
        var vmsTask = hyperV.GetVirtualMachinesAsync(cancellationToken).AsTask();

        // Host counters are optional: if they are unavailable, report null metrics
        // rather than failing the whole snapshot, so the VM list still reaches the UI.
        HostMetrics? metrics = null;
        try
        {
            metrics = await metricsTask;
        }
        catch (HyperVException ex)
        {
            loggerFactory.CreateLogger(typeof(HostEndpoints)).WarnMetricsUnavailable(ex);
        }

        try
        {
            var vms = await vmsTask;
            return TypedResults.Ok(new HostSnapshot(CreateHostInfo(hyperV), metrics, vms));
        }
        catch (HyperVException ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static HostInfo CreateHostInfo(IHyperVService hyperV) =>
        new(Environment.MachineName, hyperV.IsElevated);
}
