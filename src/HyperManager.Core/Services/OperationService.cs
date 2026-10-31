namespace HyperManager.Services;

using HyperManager.Accessors;
using HyperManager.Infrastructure.Agents;

// Executes VM operations and keeps their history
public sealed class OperationService
{
    private readonly ILogger<OperationService> log;

    private readonly IDialect dialect;

    private readonly OperationLogAccessor operationLogAccessor;

    private readonly IAgentClient agentClient;

    private readonly AgentClientOption option;

    private readonly ServiceContextProvider contextProvider;

    public OperationService(
        ILogger<OperationService> log,
        IDialect dialect,
        OperationLogAccessor operationLogAccessor,
        IAgentClient agentClient,
        AgentClientOption option,
        ServiceContextProvider contextProvider)
    {
        this.log = log;
        this.dialect = dialect;
        this.operationLogAccessor = operationLogAccessor;
        this.agentClient = agentClient;
        this.option = option;
        this.contextProvider = contextProvider;
    }

    //--------------------------------------------------------------------------------
    // Execute
    //--------------------------------------------------------------------------------

    public async ValueTask<OperationResult> ExecuteAsync(string hostName, VmInfo vm, VmOperation operation, CancellationToken cancellationToken = default)
    {
        var context = contextProvider.Current;

        var error = default(string);
        var host = option.Hosts.FirstOrDefault(x => x.Name == hostName);
        if (host is null)
        {
            error = $"ホストが見つかりません: {hostName}";
        }
        else
        {
            try
            {
                await agentClient.ExecuteAsync(host, vm.Id, operation, cancellationToken);
            }
            catch (AgentException ex)
            {
                error = ex.Message;
            }
        }

        // Audit
        var success = error is null;
        await operationLogAccessor.InsertAsync(context.Now.DateTime, context.UserId, hostName, vm.Id, vm.Name, operation.ToString(), success, error);
        if (success)
        {
            log.InfoOperationSucceeded(context.UserId, hostName, vm.Name, operation);
        }
        else
        {
            log.WarnOperationFailed(context.UserId, hostName, vm.Name, operation, error);
        }

        return new OperationResult(success, error);
    }

    //--------------------------------------------------------------------------------
    // History
    //--------------------------------------------------------------------------------

    public async ValueTask<PagedResult<OperationLogEntity>> QueryHistoryPageAsync(string? keyword, int page, int size, CancellationToken cancellationToken = default)
    {
        var pattern = dialect.Match(keyword);
        var total = await operationLogAccessor.CountAsync(pattern, cancellationToken);
        var items = await operationLogAccessor.QueryPageAsync(pattern, size, page * size, cancellationToken);
        return new PagedResult<OperationLogEntity>(total, page, size, items);
    }

    public IAsyncEnumerable<OperationLogEntity> QueryHistoryExportEnumerable(CancellationToken cancellationToken) =>
        operationLogAccessor.QueryExportEnumerable(cancellationToken);
}
