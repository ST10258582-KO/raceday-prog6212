using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.Tests.Helpers;

namespace RaceDay.Tests;

/// <summary>
/// Tests for event filtering via query parameters (GET /api/events?status=&amp;city=).
/// Verifies that status and city filters work correctly.
/// </summary>
public class EventFilterTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> RegisterOrgAsync()
    {
        var email = $"org_{Guid.NewGuid()}@filtertest.com";
        var resp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Filter Org", email, "Password123!", "Organiser", null, null));
        return (await resp.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
    }

    [Fact]
    public async Task GetEvents_FilterByStatus_ReturnsMatchingEvents()
    {
        var token = await RegisterOrgAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create an Open event
        await _client.PostAsJsonAsync("/api/events",
            new CreateEventRequest("Open Event Filter Test", null, new DateOnly(2028, 1, 1),
                "Venue A", "Pretoria", "Gauteng", null));

        _client.DefaultRequestHeaders.Authorization = null;

        // Filter by Upcoming (default status for new events)
        var response = await _client.GetAsync("/api/events?status=Upcoming");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.NotNull(events);
        Assert.All(events, e => Assert.Equal("Upcoming", e.Status));
    }

    [Fact]
    public async Task GetEvents_FilterByCity_ReturnsMatchingEvents()
    {
        var token = await RegisterOrgAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/events",
            new CreateEventRequest("City Filter Test", null, new DateOnly(2028, 2, 1),
                "Venue B", "Port Elizabeth", "Eastern Cape", null));

        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/events?city=Port+Elizabeth");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.NotNull(events);
        Assert.All(events, e => Assert.Contains("Port Elizabeth", e.City));
    }
}
