namespace HyperManager;

using System.Diagnostics.Metrics;

using HyperManager.Host.Application.Telemetry;

public sealed class MetricsTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    private long count;

    public MetricsTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ApiRequestIsCounted()
    {
        // Arrange
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if ((instrument.Meter.Name == Source.Name) && (instrument.Name == "api.request.execution"))
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) => Interlocked.Add(ref count, measurement));
        listener.Start();

        var client = factory.CreateClient();
        await TestApplicationFactory.LoginAsync(client, TestContext.Current.CancellationToken);

        // Act
        var response = await client.GetAsync(new Uri("/api/operations", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(Interlocked.Read(ref count) >= 1);
    }
}
