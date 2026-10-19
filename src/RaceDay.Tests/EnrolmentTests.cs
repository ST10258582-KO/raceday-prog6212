using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.Tests.Helpers;

namespace RaceDay.Tests;

/// <summary>
/// Tests for event enrolments (POST/GET/DELETE /api/enrolments).
/// Verifies Participants can enrol, cannot double-enrol, and Organisers cannot enrol.
/// </summary>
public class EnrolmentTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> RegisterAndLoginAsync(string role)
    {
        var email = $"{role.ToLower()}_{Guid.NewGuid()}@enroltest.com";
        var regResp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"Enrol {role}", email, "Password123!", role, null, null));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    private async Task<(int EventID, int CategoryID)> CreateEventWithCategoryAsync(string orgToken)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);

        var ev = await (await _client.PostAsJsonAsync("/api/events",
            new CreateEventRequest("Enrol Test Event", null, new DateOnly(2027, 7, 1),
                "Test Location", "Test City", "Gauteng", null)))
            .Content.ReadFromJsonAsync<EventResponse>();

        var cat = await (await _client.PostAsJsonAsync($"/api/events/{ev!.EventID}/categories",
            new CreateCategoryRequest("5K Test", 5m, "km", 100m, 100, "Running")))
            .Content.ReadFromJsonAsync<CategoryResponse>();

        // Open the event so participants can enrol
        await _client.PutAsJsonAsync($"/api/events/{ev.EventID}",
            new UpdateEventRequest(null, null, null, null, null, null, "Open", null));

        _client.DefaultRequestHeaders.Authorization = null;
        return (ev.EventID, cat!.CategoryID);
    }

    [Fact]
    public async Task Enrol_AsParticipant_Returns201()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(orgToken);

        var partToken = await RegisterAndLoginAsync("Participant");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", partToken);

        var response = await _client.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest(categoryId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.NotNull(body);
        Assert.Equal(categoryId, body.CategoryID);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(orgToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);
        var response = await _client.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest(categoryId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Enrol_Twice_SameCategory_Returns400()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(orgToken);

        var partToken = await RegisterAndLoginAsync("Participant");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", partToken);

        await _client.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest(categoryId));
        var response = await _client.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest(categoryId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetMyEnrolments_AsParticipant_ReturnsOwnEnrolments()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(orgToken);

        var partToken = await RegisterAndLoginAsync("Participant");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", partToken);

        await _client.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest(categoryId));

        var response = await _client.GetAsync("/api/enrolments/my");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.NotNull(body);
        Assert.Single(body);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Enrol_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest(1));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
