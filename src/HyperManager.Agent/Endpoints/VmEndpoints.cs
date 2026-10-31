namespace HyperManager.Agent.Endpoints;

public static class VmEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapVmEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Vms)
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/", HandleListAsync)
            .WithName("VmList")
            .Produces<IReadOnlyList<VmInfo>>()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        MapOperation(group.MapPost("/{id}/start", HandleStartAsync), "VmStart");
        MapOperation(group.MapPost("/{id}/resume", HandleResumeAsync), "VmResume");
        MapOperation(group.MapPost("/{id}/pause", HandlePauseAsync), "VmPause");
        MapOperation(group.MapPost("/{id}/save", HandleSaveAsync), "VmSave");
        MapOperation(group.MapPost("/{id}/shutdown", HandleShutdownAsync), "VmShutdown");
        MapOperation(group.MapPost("/{id}/turnoff", HandleTurnOffAsync), "VmTurnOff");
    }

    private static void MapOperation(RouteHandlerBuilder builder, string name) =>
        builder
            .WithName(name)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    //--------------------------------------------------------------------------------
    // List
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleListAsync(
        IHyperVService hyperV,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await hyperV.GetVirtualMachinesAsync(cancellationToken));
        }
        catch (HyperVException ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private static ValueTask<IResult> HandleStartAsync(string id, IHyperVService hyperV, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        ExecuteAsync(loggerFactory, VmOperation.Start, id, () => hyperV.StartAsync(id, cancellationToken));

    private static ValueTask<IResult> HandleResumeAsync(string id, IHyperVService hyperV, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        ExecuteAsync(loggerFactory, VmOperation.Resume, id, () => hyperV.ResumeAsync(id, cancellationToken));

    private static ValueTask<IResult> HandlePauseAsync(string id, IHyperVService hyperV, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        ExecuteAsync(loggerFactory, VmOperation.Pause, id, () => hyperV.PauseAsync(id, cancellationToken));

    private static ValueTask<IResult> HandleSaveAsync(string id, IHyperVService hyperV, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        ExecuteAsync(loggerFactory, VmOperation.Save, id, () => hyperV.SaveAsync(id, cancellationToken));

    // The request body is optional (force = false)
    private static ValueTask<IResult> HandleShutdownAsync(string id, ShutdownRequest? request, IHyperVService hyperV, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        ExecuteAsync(loggerFactory, VmOperation.Shutdown, id, () => hyperV.ShutdownAsync(id, request?.Force ?? false, cancellationToken));

    private static ValueTask<IResult> HandleTurnOffAsync(string id, IHyperVService hyperV, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        ExecuteAsync(loggerFactory, VmOperation.TurnOff, id, () => hyperV.TurnOffAsync(id, cancellationToken));

    private static async ValueTask<IResult> ExecuteAsync(ILoggerFactory loggerFactory, VmOperation operation, string id, Func<ValueTask> action)
    {
        var log = loggerFactory.CreateLogger(typeof(VmEndpoints));
        try
        {
            await action();
            log.InfoOperationSucceeded(operation, id);
            return TypedResults.NoContent();
        }
        catch (VmNotFoundException ex)
        {
            log.WarnOperationFailed(operation, id, ex.Message);
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (HyperVException ex)
        {
            log.WarnOperationFailed(operation, id, ex.Message);
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
