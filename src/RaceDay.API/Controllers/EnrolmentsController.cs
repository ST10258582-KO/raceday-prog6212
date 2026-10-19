using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>
/// Manages participant enrolments into event categories.
/// </summary>
[ApiController]
[Route("api/enrolments")]
[Authorize]
[Produces("application/json")]
public class EnrolmentsController(RaceDayContext db) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static EnrolmentResponse ToResponse(Enrolment en) =>
        new(en.EnrolmentID, en.ParticipantID, en.Participant.FullName,
            en.CategoryID, en.Category.Name, en.Category.EventID, en.Category.Event.Name,
            en.EnrolmentDate, en.PaymentStatus, en.BibNumber);

    /// <summary>Enrols the authenticated Participant into a specific event category.</summary>
    /// <response code="201">Enrolment created with payment status.</response>
    /// <response code="400">Already enrolled or category is full.</response>
    /// <response code="403">Requires Participant role.</response>
    /// <response code="404">Category not found.</response>
    [HttpPost]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enrol([FromBody] CreateEnrolmentRequest req)
    {
        var category = await db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == req.CategoryID);
        if (category == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Category not found." });

        // Participants may only enrol in events that are currently accepting entries
        var allowedStatuses = new[] { "Open" };
        if (!allowedStatuses.Contains(category.Event.Status))
            return BadRequest(new { status = 400, error = "Bad Request", message = "This event is not currently open for enrolments." });

        var alreadyEnrolled = await db.Enrolments
            .AnyAsync(en => en.ParticipantID == CurrentUserId && en.CategoryID == req.CategoryID);
        if (alreadyEnrolled)
            return BadRequest(new { status = 400, error = "Bad Request", message = "You are already enrolled in this category." });

        if (category.MaxParticipants.HasValue)
        {
            var count = await db.Enrolments.CountAsync(en => en.CategoryID == req.CategoryID);
            if (count >= category.MaxParticipants.Value)
                return BadRequest(new { status = 400, error = "Bad Request", message = "This category is full." });
        }

        var enrolment = new Enrolment
        {
            ParticipantID = CurrentUserId,
            CategoryID = req.CategoryID
        };

        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();

        await db.Entry(enrolment).Reference(en => en.Participant).LoadAsync();
        await db.Entry(enrolment).Reference(en => en.Category).LoadAsync();
        await db.Entry(enrolment.Category).Reference(c => c.Event).LoadAsync();

        return CreatedAtAction(nameof(GetEnrolment), new { id = enrolment.EnrolmentID }, ToResponse(enrolment));
    }

    /// <summary>Returns all enrolments belonging to the authenticated Participant.</summary>
    /// <response code="200">Array of enrolment objects including event and category details.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet("my")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var enrolments = await db.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Category).ThenInclude(c => c.Event)
            .Where(en => en.ParticipantID == CurrentUserId)
            .ToListAsync();

        return Ok(enrolments.Select(ToResponse));
    }

    /// <summary>Returns a single enrolment. Participants may only access their own; Organisers may access any.</summary>
    /// <response code="200">Enrolment object.</response>
    /// <response code="403">Accessing another user's enrolment.</response>
    /// <response code="404">Enrolment not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrolment(int id)
    {
        var enrolment = await db.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(en => en.EnrolmentID == id);
        if (enrolment == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Enrolment not found." });

        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == "Participant" && enrolment.ParticipantID != CurrentUserId)
            return Forbid();

        return Ok(ToResponse(enrolment));
    }

    /// <summary>Cancels a Participant's enrolment. Participants may only cancel their own.</summary>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="403">Not the enrolled participant.</response>
    /// <response code="404">Enrolment not found.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await db.Enrolments.FindAsync(id);
        if (enrolment == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Enrolment not found." });
        if (enrolment.ParticipantID != CurrentUserId)
            return Forbid();

        db.Enrolments.Remove(enrolment);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
