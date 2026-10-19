using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>
/// Manages race results captured by Organisers after events complete.
/// </summary>
[ApiController]
[Route("api/results")]
[Authorize]
[Produces("application/json")]
public class ResultsController(RaceDayContext db) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static ResultResponse ToResponse(Result r) =>
        new(r.ResultID, r.EnrolmentID, r.Enrolment.Participant.FullName,
            r.Enrolment.Category.Event.Name, r.Enrolment.Category.Name,
            r.FinishTime, r.Position, r.Status, r.Notes, r.RecordedAt);

    /// <summary>Records a result for a specific enrolment after the event. Organiser must own the event.</summary>
    /// <response code="201">Result created.</response>
    /// <response code="400">Result already exists for this enrolment.</response>
    /// <response code="403">Requires Organiser role or not the event owner.</response>
    /// <response code="404">Enrolment not found.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateResult([FromBody] CreateResultRequest req)
    {
        var enrolment = await db.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(en => en.EnrolmentID == req.EnrolmentID);
        if (enrolment == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Enrolment not found." });

        if (enrolment.Category.Event.OrganiserID != CurrentUserId)
            return Forbid();

        if (await db.Results.AnyAsync(r => r.EnrolmentID == req.EnrolmentID))
            return BadRequest(new { status = 400, error = "Bad Request", message = "A result already exists for this enrolment." });

        var validStatuses = new[] { "Finished", "DNF", "DNS", "DQ" };
        if (!validStatuses.Contains(req.Status))
            return BadRequest(new { status = 400, error = "Bad Request", message = "Status must be 'Finished', 'DNF', 'DNS', or 'DQ'." });

        if (req.Position.HasValue && req.Position.Value <= 0)
            return BadRequest(new { status = 400, error = "Bad Request", message = "Position must be a positive integer." });

        var result = new Result
        {
            EnrolmentID = req.EnrolmentID,
            FinishTime = req.FinishTime,
            Position = req.Position,
            Status = req.Status,
            Notes = req.Notes
        };

        db.Results.Add(result);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(result));
    }

    /// <summary>Updates an existing result record. Organiser must own the event.</summary>
    /// <response code="200">Updated result object.</response>
    /// <response code="403">Not the event owner.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateResult(int id, [FromBody] UpdateResultRequest req)
    {
        var result = await db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.Participant)
            .Include(r => r.Enrolment).ThenInclude(en => en.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.ResultID == id);
        if (result == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Result not found." });

        if (result.Enrolment.Category.Event.OrganiserID != CurrentUserId)
            return Forbid();

        var validStatuses = new[] { "Finished", "DNF", "DNS", "DQ" };
        if (req.Status != null && !validStatuses.Contains(req.Status))
            return BadRequest(new { status = 400, error = "Bad Request", message = "Status must be 'Finished', 'DNF', 'DNS', or 'DQ'." });

        if (req.Position.HasValue && req.Position.Value <= 0)
            return BadRequest(new { status = 400, error = "Bad Request", message = "Position must be a positive integer." });

        if (req.FinishTime.HasValue) result.FinishTime = req.FinishTime;
        if (req.Position.HasValue) result.Position = req.Position;
        if (req.Status != null) result.Status = req.Status;
        if (req.Notes != null) result.Notes = req.Notes;

        await db.SaveChangesAsync();
        return Ok(ToResponse(result));
    }

    /// <summary>Returns all results for the authenticated Participant's past enrolments.</summary>
    /// <response code="200">Array of result objects with event and category context.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet("my")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyResults()
    {
        var results = await db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.Participant)
            .Include(r => r.Enrolment).ThenInclude(en => en.Category).ThenInclude(c => c.Event)
            .Where(r => r.Enrolment.ParticipantID == CurrentUserId)
            .ToListAsync();

        return Ok(results.Select(ToResponse));
    }
}
