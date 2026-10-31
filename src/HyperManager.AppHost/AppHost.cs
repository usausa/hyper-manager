// ReSharper disable StringLiteralTypo
using System.Security.Cryptography;

var builder = DistributedApplication.CreateBuilder(args);

// API key shared by the agent and the host (generated for each run)
var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
var apiKey = builder.AddParameter("agent-api-key", () => key, secret: true);

// Hyper-V agent of this machine (run Visual Studio as administrator to operate VMs)
var agent = builder.AddProject<Projects.HyperManager_Agent>("agent")
    .WithEnvironment("Agent__ApiKey", apiKey)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.HyperManager_Host>("hypermanager")
    .WithEnvironment("Agent__Hosts__0__Name", "Local")
    .WithEnvironment("Agent__Hosts__0__Url", agent.GetEndpoint("http"))
    .WithEnvironment("Agent__Hosts__0__ApiKey", apiKey)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
