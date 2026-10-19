using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.Tests.Helpers;

namespace RaceDay.Tests;

/// <summary>
/// Tests for event route endpoints (GET/POST/PUT /api/events/{id}/route).
/// Verifies public GET access and Organiser-only create/update.
/// </summary>
public class RouteTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> RegisterAndLoginAsync(string role)
    {
        var email = $"{role.ToLower()}_{Guid.NewGuid()}@routetest.com";
        var resp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"Route {role}", email, "Password123!", role, null, null));
        return (await resp.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
    }

    private async Task<EventResponse> CreateEventAsync(string orgToken)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);
        var ev = await (await _client.PostAsJsonAsync("/api/events",
            new CreateEventRequest("Route Test Event", null, new DateOnly(2027, 10, 1),
                "Test Venue", "Cape Town", "Western Cape", null)))
            .Content.ReadFromJsonAsync<EventResponse>();
        _client.DefaultRequestHeaders.Authorization = null;
        return ev!;
    }

    [Fact]
    public async Task GetRoute_NoRoute_Returns404()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var ev = await CreateEventAsync(orgToken);

        var response = await _client.GetAsync($"/api/events/{ev.EventID}/route");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoute_AsOwner_Returns201()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var ev = await CreateEventAsync(orgToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);
        var response = await _client.PostAsJsonAsync($"/api/events/{ev.EventID}/route",
            new CreateRouteRequest("Start at Sea Point", "https://maps.example.com", null, 45.5m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RouteResponse>();
        Assert.NotNull(body);
        Assert.Equal(45.5m, body.ElevationGain);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task CreateRoute_Duplicate_Returns400()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var ev = await CreateEventAsync(orgToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);
        await _client.PostAsJsonAsync($"/api/events/{ev.EventID}/route",
            new CreateRouteRequest("Route 1", null, null, null));

        var response = await _client.PostAsJsonAsync($"/api/events/{ev.EventID}/route",
            new CreateRouteRequest("Route 2", null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetRoute_AfterCreation_Returns200()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var ev = await CreateEventAsync(orgToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);
        await _client.PostAsJsonAsync($"/api/events/{ev.EventID}/route",
            new CreateRouteRequest("Beachfront run", "https://maps.example.com/route", null, 12.5m));
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/events/{ev.EventID}/route");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RouteResponse>();
        Assert.Equal("Beachfront run", body!.RouteDescription);
    }
}
