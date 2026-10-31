namespace HyperManager.Host.Endpoints;

using Microsoft.AspNetCore.Http.HttpResults;

using Smart.Mapper;

//--------------------------------------------------------------------------------
// Models
//--------------------------------------------------------------------------------

public sealed class OperationListEntry
{
    public long Id { get; set; }

    public DateTime ExecutedAt { get; set; }

    public string UserId { get; set; } = default!;

    public string HostName { get; set; } = default!;

    public string VmId { get; set; } = default!;

    public string VmName { get; set; } = default!;

    public string Operation { get; set; } = default!;

    public bool Success { get; set; }

    public string? Message { get; set; }
}

public sealed class OperationListResponse
{
    public int Total { get; set; }

    public int Page { get; set; }

    public int Size { get; set; }

    public IReadOnlyList<OperationListEntry> Items { get; set; } = default!;
}

//--------------------------------------------------------------------------------
// Mapper
//--------------------------------------------------------------------------------

public static partial class OperationMapper
{
    [Mapper]
    public static partial OperationListEntry ToListEntry(this OperationLogEntity entity);
}

//--------------------------------------------------------------------------------
// Endpoints
//--------------------------------------------------------------------------------

public static class OperationEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapOperationEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup(ApiRoutes.Operations)
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/", HandleListAsync)
            .WithName("OperationList")
            .Produces<OperationListResponse>()
            .ProducesValidationProblem();
        group.MapGet("/csv", HandleExportCsv)
            .WithName("OperationExport")
            .Produces<Stream>(StatusCodes.Status200OK, "text/csv");
    }

    //--------------------------------------------------------------------------------
    // List
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleListAsync(
        OperationService operationService,
        [MaxLength(Length.Keyword)] string? keyword,
        CancellationToken cancellationToken,
        [Range(0, Int32.MaxValue)] int page = 0,
        [Range(1, 100)] int size = 20)
    {
        var result = await operationService.QueryHistoryPageAsync(keyword, page, size, cancellationToken);
        return TypedResults.Ok(new OperationListResponse
        {
            Total = result.Total,
            Page = result.Page,
            Size = result.Size,
            Items = result.Items.Select(static x => x.ToListEntry()).ToList()
        });
    }

    //--------------------------------------------------------------------------------
    // Export
    //--------------------------------------------------------------------------------

    private static PushStreamHttpResult HandleExportCsv(OperationService operationService, CancellationToken cancellationToken) =>
        TypedResults.Stream(
            async stream =>
            {
                await using var writer = new StreamWriter(stream, new UTF8Encoding(true));
                await using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
                await csv.WriteRecordsAsync(operationService.QueryHistoryExportEnumerable(cancellationToken), cancellationToken);
            },
            "text/csv",
            "operations.csv");
}
