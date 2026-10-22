using System.Net;
using System.Net.Http.Json;
using FourDotnet.BoogaBooster.DigitalTwin.Abstractions;
using FourDotnet.BoogaBooster.DigitalTwin.Application;
using FourDotnet.BoogaBooster.DigitalTwin.Endpoints;
using FourDotnet.BoogaBooster.DigitalTwin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FourDotnet.BoogaBooster.DigitalTwin.Tests;

/// <summary>HTTP coverage for the module-owned cycle-duration configuration route.</summary>
public sealed class DigitalTwinCycleDurationEndpointTests
{
    [Fact]
    public async Task Cycle_duration_endpoint_dispatches_configuration_and_returns_client_errors_for_rejections()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.AddDigitalTwinModule();
        builder.Services.RemoveAll<IHostedService>();

        await using var app = builder.Build();
        app.MapDigitalTwinEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var store = app.Services.GetRequiredService<IRideStore>();
        var server = app.Services.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        Assert.NotNull(addressesFeature);
        var address = Assert.Single(addressesFeature.Addresses);

        using var client = new HttpClient { BaseAddress = new Uri(address) };
        var accepted = await client.PostAsJsonAsync(
            "/ride/cycle-duration",
            new { durationMinutes = 12 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        Assert.Equal(12L, store.GetTelemetry().CycleDurationMinutes);
        var telemetry = await client.GetFromJsonAsync<RideTelemetry>(
            "/ride/telemetry",
            TestContext.Current.CancellationToken);
        Assert.NotNull(telemetry);
        Assert.Equal(12L, telemetry.CycleDurationMinutes);
        Assert.Equal(0d, telemetry.RemainingCycleTimeSeconds);
        Assert.Equal(0d, telemetry.StopRampProgress);
        Assert.Equal(0d, telemetry.BrakeApplication);

        var invalid = await client.PostAsJsonAsync(
            "/ride/cycle-duration",
            new { durationMinutes = 0 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(12L, store.GetTelemetry().CycleDurationMinutes);

        store.RequestStateTransition(RideState.Loading);
        store.RequestStateTransition(RideState.Safe);
        store.RequestStateTransition(RideState.Started);

        var running = await client.PostAsJsonAsync(
            "/ride/cycle-duration",
            new { durationMinutes = 15 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, running.StatusCode);
        Assert.Equal(12L, store.GetTelemetry().CycleDurationMinutes);

        store.RequestStateTransition(RideState.Stopping);
        var stopping = await client.PostAsJsonAsync(
            "/ride/cycle-duration",
            new { durationMinutes = 18 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, stopping.StatusCode);
        Assert.Equal(12L, store.GetTelemetry().CycleDurationMinutes);

        store.RequestStateTransition(RideState.EmergencyStop);
        var emergency = await client.PostAsJsonAsync(
            "/ride/cycle-duration",
            new { durationMinutes = 21 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, emergency.StatusCode);
        Assert.Equal(12L, store.GetTelemetry().CycleDurationMinutes);
    }
}
