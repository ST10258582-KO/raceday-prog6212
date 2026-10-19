using System.Net;
using System.Net.Http.Json;
using RaceDay.API.DTOs;
using RaceDay.Tests.Helpers;

namespace RaceDay.Tests;

/// <summary>
/// Tests for user registration and login (POST /api/auth/register and /api/auth/login).
/// Verifies authenticated and unauthenticated scenarios for both Organiser and Participant roles.
/// </summary>
public class AuthTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidOrganiser_Returns201WithToken()
    {
        var req = new RegisterRequest("Test Organiser", $"org_{Guid.NewGuid()}@test.com",
            "Password123!", "Organiser", null, null);

        var response = await _client.PostAsJsonAsync("/api/auth/register", req);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Organiser", body.Role);
        Assert.False(string.IsNullOrEmpty(body.Token));
    }

    [Fact]
    public async Task Register_WithValidParticipant_Returns201WithToken()
    {
        var req = new RegisterRequest("Test Participant", $"part_{Guid.NewGuid()}@test.com",
            "Password123!", "Participant", null, null);

        var response = await _client.PostAsJsonAsync("/api/auth/register", req);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Participant", body.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var email = $"dup_{Guid.NewGuid()}@test.com";
        var req = new RegisterRequest("User One", email, "Password123!", "Participant", null, null);

        await _client.PostAsJsonAsync("/api/auth/register", req);
        var response = await _client.PostAsJsonAsync("/api/auth/register", req);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_Returns400()
    {
        var req = new RegisterRequest("Bad Role", $"badrole_{Guid.NewGuid()}@test.com",
            "Password123!", "Admin", null, null);

        var response = await _client.PostAsJsonAsync("/api/auth/register", req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        var email = $"login_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Login User", email, "Password123!", "Participant", null, null));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "Password123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.Token));
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = $"wrongpw_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Wrong PW", email, "Password123!", "Participant", null, null));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("nobody@unknown.com", "Password123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
