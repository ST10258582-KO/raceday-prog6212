using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.Tests.Helpers;

namespace RaceDay.Tests;

/// <summary>
/// Tests for category endpoints (GET/POST/PUT/DELETE /api/events/{id}/categories).
/// Verifies Organisers can manage categories and public access to listing.
/// </summary>
public class CategoryTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> RegisterAndLoginAsync(string role)
    {
        var email = $"{role.ToLower()}_{Guid.NewGuid()}@cattest.com";
        var resp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"Cat {role}", email, "Password123!", role, null, null));
        return (await resp.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
    }

    private async Task<EventResponse> CreateEventAsync(string orgToken)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);
        var ev = await (await _client.PostAsJsonAsync("/api/events",
            new CreateEventRequest("Cat Test Event", null, new DateOnly(2027, 9, 1),
                "Venue", "Durban", "KwaZulu-Natal", null)))
            .Content.ReadFromJsonAsync<EventResponse>();
        _client.DefaultRequestHeaders.Authorization = null;
        return ev!;
    }

    [Fact]
    public async Task GetCategories_PublicAccess_Returns200()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var ev = await CreateEventAsync(orgToken);

        var response = await _client.GetAsync($"/api/events/{ev.EventID}/categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsOwner_Returns201()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var ev = await CreateEventAsync(orgToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);
        var response = await _client.PostAsJsonAsync($"/api/events/{ev.EventID}/categories",
            new CreateCategoryRequest("21K Run", 21.1m, "km", 350m, 500, "Running"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.Equal("21K Run", body!.Name);
        Assert.Equal(21.1m, body.Distance);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_Returns403()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var partToken = await RegisterAndLoginAsync("Participant");
        var ev = await CreateEventAsync(orgToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", partToken);
        var response = await _client.PostAsJsonAsync($"/api/events/{ev.EventID}/categories",
            new CreateCategoryRequest("5K Walk", 5m, "km", 100m, null, "Walking"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task CreateCategory_NonOwnerOrganiser_Returns403()
    {
        var owner = await RegisterAndLoginAsync("Organiser");
        var other = await RegisterAndLoginAsync("Organiser");
        var ev = await CreateEventAsync(owner);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other);
        var response = await _client.PostAsJsonAsync($"/api/events/{ev.EventID}/categories",
            new CreateCategoryRequest("Cycling 30K", 30m, "km", 200m, null, "Cycling"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task DeleteCategory_ByOwner_Returns204()
    {
        var orgToken = await RegisterAndLoginAsync("Organiser");
        var ev = await CreateEventAsync(orgToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", orgToken);
        var cat = await (await _client.PostAsJsonAsync($"/api/events/{ev.EventID}/categories",
            new CreateCategoryRequest("10K Race", 10m, "km", 250m, null, "Running")))
            .Content.ReadFromJsonAsync<CategoryResponse>();

        var response = await _client.DeleteAsync($"/api/events/{ev.EventID}/categories/{cat!.CategoryID}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }
}
