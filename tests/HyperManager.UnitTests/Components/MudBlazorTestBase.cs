namespace HyperManager.Components;

using Bunit;

using HyperManager.Host.Application.Context;
using HyperManager.Services;

using Microsoft.Extensions.DependencyInjection;

using MudBlazor.Services;

public abstract class MudBlazorTestBase : BunitContext
{
    protected MudBlazorTestBase()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<ApplicationServiceContextProvider>();
        Services.AddSingleton<ServiceContextProvider>(static p => p.GetRequiredService<ApplicationServiceContextProvider>());
        Services.AddScoped<BlazorServiceScope>();
    }
}
