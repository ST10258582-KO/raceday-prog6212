using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.Tests.Helpers;

namespace RaceDay.Tests;

/// <summary>
/// Tests that enrolment is rejected for events that are not Open or Upcoming.
/// </summary>
public class EnrolmentStatusTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> RegisterAsync(string role)
    {
        var email = $"{role.ToLower()}_{Guid.NewGuid()}@statustest.com";
        var resp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"Status {role}", email, "Password123!", role, null, null));
        return (await resp.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
    }

    [Fact]
    public async Task Enrol_ClosedEvent_Returns400()
    {
        var orgToken = await RegisterAsync("Organiser");
        var partToken = await RegisterAsync("Participant");

        // Create event and category
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);
        var ev = await (await _client.PostAsJsonAsync("/api/events",
            new CreateEventRequest("Closed Event Test", null, new DateOnly(2027, 11, 1),
                "Venue", "Bloemfontein", "Free State", null)))
            .Content.ReadFromJsonAsync<EventResponse>();

        var cat = await (await _client.PostAsJsonAsync($"/api/events/{ev!.EventID}/categories",
            new CreateCategoryRequest("5K", 5m, "km", 100m, null, "Running")))
            .Content.ReadFromJsonAsync<CategoryResponse>();

        // Close the event
        await _client.PutAsJsonAsync($"/api/events/{ev.EventID}",
            new UpdateEventRequest(null, null, null, null, null, null, "Closed", null));

        // Try to enrol as participant
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", partToken);
        var response = await _client.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest(cat!.CategoryID));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }
}
