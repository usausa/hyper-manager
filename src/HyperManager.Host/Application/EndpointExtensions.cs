namespace HyperManager.Host.Application;

using HyperManager.Host.Application.Context;
using HyperManager.Host.Application.Telemetry;

public static class EndpointExtensions
{
    public static RouteGroupBuilder MapApiGroup(this IEndpointRouteBuilder endpoints, string prefix) =>
        endpoints.MapGroup(prefix)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<RequestMetricsEndpointFilter>()
            .AddEndpointFilter<ServiceContextEndpointFilter>();
}
