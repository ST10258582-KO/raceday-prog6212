using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.Tests.Helpers;

namespace RaceDay.Tests;

/// <summary>
/// Tests for user profile endpoints (GET/PUT /api/users/me).
/// Verifies authenticated and unauthenticated access.
/// </summary>
public class UserProfileTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(string Token, int UserID)> RegisterAsync(string role)
    {
        var email = $"{role.ToLower()}_{Guid.NewGuid()}@profiletest.com";
        var regResp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"Profile {role}", email, "Password123!", role, "0821234567", new DateOnly(1995, 1, 1)));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();
        return (auth!.Token, auth.UserID);
    }

    [Fact]
    public async Task GetProfile_Authenticated_Returns200WithUserData()
    {
        var (token, userId) = await RegisterAsync("Participant");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(body);
        Assert.Equal(userId, body.UserID);
        Assert.Equal("Participant", body.Role);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetProfile_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ValidData_Returns200WithUpdatedData()
    {
        var (token, _) = await RegisterAsync("Organiser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync("/api/users/me",
            new UpdateProfileRequest("Updated Name", "0829876543", new DateOnly(1990, 6, 15)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal("Updated Name", body!.FullName);
        Assert.Equal("0829876543", body.PhoneNumber);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task UpdateProfile_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PutAsJsonAsync("/api/users/me",
            new UpdateProfileRequest("Test", null, null));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
