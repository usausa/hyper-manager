using Microsoft.Extensions.Hosting.WindowsServices;

//--------------------------------------------------------------------------------
// Configure builder
//--------------------------------------------------------------------------------
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
});

// System
builder.ConfigureSystem();

// Host
builder.ConfigureHost();

// Logging
builder.ConfigureLogging();

// API
builder.ConfigureApi();
// Authentication
builder.ConfigureAuthentication();
// OpenApi
builder.ConfigureOpenApi();

// Health
builder.ConfigureHealth();
// Metrics
builder.ConfigureTelemetry();

// Components
builder.ConfigureComponents();

//--------------------------------------------------------------------------------
// Configure the HTTP request pipeline
//--------------------------------------------------------------------------------
var app = builder.Build();

// Startup information
app.LogStartupInformation();

// Error handler
app.UseErrorHandler();

// Routing
app.UseRouting();

// HTTP log
app.UseHttpLog();

// Authentication
app.UseAuthentication();
app.UseAuthorization();

// End point
app.MapEndpoints();

// Run
await app.RunAsync();

[ExcludeFromCodeCoverage]
public partial class Program;
