using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.Tests.Helpers;

namespace RaceDay.Tests;

/// <summary>
/// Tests for results management (POST/PUT /api/results, GET /api/results/my).
/// Verifies Organisers can record results and Participants can view their own.
/// </summary>
public class ResultTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> RegisterAndLoginAsync(string role, string? suffix = null)
    {
        var tag = suffix ?? Guid.NewGuid().ToString();
        var email = $"{role.ToLower()}_{tag}@resulttest.com";
        var regResp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"Result {role}", email, "Password123!", role, null, null));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    private async Task<(int EnrolmentID, string OrgToken, string PartToken)> SetupEnrolmentAsync()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var partToken = await RegisterAndLoginAsync("Participant");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);

        var ev = await (await _client.PostAsJsonAsync("/api/events",
            new CreateEventRequest("Result Test Event", null, new DateOnly(2027, 8, 1),
                "Test Venue", "Johannesburg", "Gauteng", null)))
            .Content.ReadFromJsonAsync<EventResponse>();

        var cat = await (await _client.PostAsJsonAsync($"/api/events/{ev!.EventID}/categories",
            new CreateCategoryRequest("10K Test", 10m, "km", 200m, 50, "Running")))
            .Content.ReadFromJsonAsync<CategoryResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", partToken);
        var enrolment = await (await _client.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest(cat!.CategoryID)))
            .Content.ReadFromJsonAsync<EnrolmentResponse>();

        _client.DefaultRequestHeaders.Authorization = null;
        return (enrolment!.EnrolmentID, orgToken, partToken);
    }

    [Fact]
    public async Task RecordResult_AsOrganiser_Returns201()
    {
        var (enrolmentId, orgToken, _) = await SetupEnrolmentAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);
        var response = await _client.PostAsJsonAsync("/api/results",
            new CreateResultRequest(enrolmentId, new TimeOnly(0, 52, 34), 1, "Finished", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.NotNull(body);
        Assert.Equal("Finished", body.Status);
        Assert.Equal(1, body.Position);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task RecordResult_AsParticipant_Returns403()
    {
        var (enrolmentId, _, partToken) = await SetupEnrolmentAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", partToken);
        var response = await _client.PostAsJsonAsync("/api/results",
            new CreateResultRequest(enrolmentId, null, null, "DNS", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task RecordResult_Duplicate_Returns400()
    {
        var (enrolmentId, orgToken, _) = await SetupEnrolmentAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);
        await _client.PostAsJsonAsync("/api/results",
            new CreateResultRequest(enrolmentId, new TimeOnly(1, 0, 0), 5, "Finished", null));

        var response = await _client.PostAsJsonAsync("/api/results",
            new CreateResultRequest(enrolmentId, new TimeOnly(1, 5, 0), 6, "Finished", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetMyResults_AsParticipant_ReturnsOwnResults()
    {
        var (enrolmentId, orgToken, partToken) = await SetupEnrolmentAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);
        await _client.PostAsJsonAsync("/api/results",
            new CreateResultRequest(enrolmentId, new TimeOnly(0, 45, 0), 3, "Finished", null));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", partToken);
        var response = await _client.GetAsync("/api/results/my");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.NotNull(body);
        Assert.Single(body);
        Assert.Equal("Finished", body[0].Status);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetMyResults_AsOrganiser_Returns403()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);

        var response = await _client.GetAsync("/api/results/my");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }
}
