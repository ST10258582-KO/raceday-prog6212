using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>
/// Handles user registration and login, returning JWT tokens for authenticated sessions.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(RaceDayContext db, IConfiguration config) : ControllerBase
{
    /// <summary>
    /// Registers a new user account as either an Organiser or Participant.
    /// </summary>
    /// <response code="201">User created successfully. Returns user object with JWT token.</response>
    /// <response code="400">Validation error or invalid role.</response>
    /// <response code="409">Email is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (req.Role != "Organiser" && req.Role != "Participant")
            return BadRequest(new { status = 400, error = "Bad Request", message = "Role must be 'Organiser' or 'Participant'." });

        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict(new { status = 409, error = "Conflict", message = "An account with this email already exists." });

        var user = new User
        {
            FullName = req.FullName,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role,
            PhoneNumber = req.PhoneNumber,
            DateOfBirth = req.DateOfBirth
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = GenerateToken(user);
        return CreatedAtAction(nameof(Register), new AuthResponse(user.UserID, user.FullName, user.Email, user.Role, token));
    }

    /// <summary>
    /// Authenticates an existing user and returns a JWT token.
    /// </summary>
    /// <response code="200">Login successful. Returns user object with JWT token.</response>
    /// <response code="400">Missing required fields.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == req.Email && u.IsActive);
        var passwordValid = user != null && BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
        if (!passwordValid)
            return Unauthorized(new { status = 401, error = "Unauthorized", message = "Invalid email or password." });

        var token = GenerateToken(user);
        return Ok(new AuthResponse(user.UserID, user.FullName, user.Email, user.Role, token));
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Include UserID as both Sub and NameIdentifier so controllers can resolve it via ClaimTypes.NameIdentifier
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
