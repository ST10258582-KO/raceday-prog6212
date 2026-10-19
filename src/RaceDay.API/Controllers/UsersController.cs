using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RaceDay.API.Data;
using RaceDay.API.DTOs;

namespace RaceDay.API.Controllers;

/// <summary>
/// Manages the authenticated user's own profile.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UsersController(RaceDayContext db) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Returns the authenticated user's own profile.
    /// </summary>
    /// <response code="200">User profile returned successfully.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();
        return Ok(new UserProfileResponse(user.UserID, user.FullName, user.Email, user.Role, user.PhoneNumber, user.DateOfBirth, user.CreatedAt));
    }

    /// <summary>
    /// Updates the authenticated user's own profile details.
    /// </summary>
    /// <response code="200">Profile updated successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();

        if (req.FullName != null) user.FullName = req.FullName;
        if (req.PhoneNumber != null) user.PhoneNumber = req.PhoneNumber;
        if (req.DateOfBirth.HasValue) user.DateOfBirth = req.DateOfBirth;

        await db.SaveChangesAsync();
        return Ok(new UserProfileResponse(user.UserID, user.FullName, user.Email, user.Role, user.PhoneNumber, user.DateOfBirth, user.CreatedAt));
    }
}
