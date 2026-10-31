namespace HyperManager.Accessors;

[DataAccessor]
public sealed partial class OperationLogAccessor
{
    [ExecuteScalar]
    public partial ValueTask<int> CountAsync(string? keyword, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<OperationLogEntity>> QueryPageAsync(string? keyword, int limit, int offset, CancellationToken cancellationToken);

    // ReSharper disable once CSharpWarnings::CS8424 (SDA0305がEnumeratorCancellation注釈を要求するため。実装はジェネレータ側で処理される)
    [Query]
    public partial IAsyncEnumerable<OperationLogEntity> QueryExportEnumerable([EnumeratorCancellation] CancellationToken cancellationToken);

    [ExecuteScalar]
    public partial ValueTask<long> InsertAsync(DateTime executedAt, string userId, string hostName, string vmId, string vmName, string operation, bool success, string? message);
}
