namespace HyperManager.Host.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using MudBlazor;

public sealed partial class OperationsPage
{
#pragma warning disable CA2213
    private MudDataGrid<OperationLogEntity> grid = default!;
#pragma warning restore CA2213

    private string? searchKeyword;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required OperationService OperationService { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }

    [SupplyParameterFromQuery(Name = "keyword")]
    public string? Keyword { get; set; }

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        searchKeyword = Keyword;
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private Task OnSearchKeyDown(KeyboardEventArgs args) =>
        args.Key == "Enter" ? SearchAsync() : Task.CompletedTask;

    //--------------------------------------------------------------------------------
    // Grid
    //--------------------------------------------------------------------------------

    private async Task<GridData<OperationLogEntity>> LoadServerData(GridState<OperationLogEntity> state, CancellationToken cancellationToken)
    {
        var result = await OperationService.QueryHistoryPageAsync(searchKeyword, state.Page, state.PageSize, cancellationToken);
        return new GridData<OperationLogEntity>
        {
            TotalItems = result.Total,
            Items = result.Items
        };
    }

    private Task SearchAsync()
    {
        // Sync search condition to URL
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("keyword", String.IsNullOrEmpty(searchKeyword) ? null : searchKeyword));
        return grid.ReloadServerData();
    }
}
