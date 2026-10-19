using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.Tests.Helpers;

namespace RaceDay.Tests;

/// <summary>
/// Tests for event management (GET/POST/PUT/DELETE /api/events).
/// Verifies that Organisers can manage events and Participants cannot create/edit/delete.
/// </summary>
public class EventTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> RegisterAndLoginAsync(string role)
    {
        var email = $"{role.ToLower()}_{Guid.NewGuid()}@test.com";
        var regResp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"Test {role}", email, "Password123!", role, null, null));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    private static CreateEventRequest SampleEvent() =>
        new("Cape Town 10K Test", "A test run", new DateOnly(2027, 6, 1),
            "Sea Point Promenade", "Cape Town", "Western Cape", null);

    [Fact]
    public async Task GetEvents_IsPublic_Returns200()
    {
        var response = await _client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201()
    {
        var token = await RegisterAndLoginAsync("Organiser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/events", SampleEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(body);
        Assert.Equal("Cape Town 10K Test", body.Name);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        var token = await RegisterAndLoginAsync("Participant");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/events", SampleEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task CreateEvent_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/events", SampleEvent());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ByOwner_Returns200()
    {
        var token = await RegisterAndLoginAsync("Organiser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await (await _client.PostAsJsonAsync("/api/events", SampleEvent()))
            .Content.ReadFromJsonAsync<EventResponse>();

        var updateReq = new UpdateEventRequest("Updated Name", null, null, null, null, null, "Open", null);
        var response = await _client.PutAsJsonAsync($"/api/events/{created!.EventID}", updateReq);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Updated Name", body!.Name);
        Assert.Equal("Open", body.Status);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task DeleteEvent_ByOwner_Returns204()
    {
        var token = await RegisterAndLoginAsync("Organiser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await (await _client.PostAsJsonAsync("/api/events", SampleEvent()))
            .Content.ReadFromJsonAsync<EventResponse>();

        var response = await _client.DeleteAsync($"/api/events/{created!.EventID}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetEvent_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/events/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
