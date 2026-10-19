using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>
/// Manages RaceDay events — listing, creating, updating, deleting, and viewing enrolments/results.
/// </summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController(RaceDayContext db) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static EventResponse ToResponse(Event ev, bool includeCategories = false) =>
        new(ev.EventID, ev.OrganiserID, ev.Organiser?.FullName ?? string.Empty,
            ev.Name, ev.Description, ev.EventDate, ev.Location, ev.City, ev.Province,
            ev.Status, ev.ImageURL, ev.CreatedAt,
            includeCategories ? ev.Categories.Select(c => new CategoryResponse(
                c.CategoryID, c.EventID, c.Name, c.Distance, c.DistanceUnit,
                c.EntryFee, c.MaxParticipants, c.EventType, c.CreatedAt)) : null);

    /// <summary>Returns all upcoming and open events. Public access.</summary>
    /// <response code="200">Array of event objects.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents()
    {
        var events = await db.Events
            .Include(e => e.Organiser)
            .OrderBy(e => e.EventDate)
            .ToListAsync();
        return Ok(events.Select(e => ToResponse(e)));
    }

    /// <summary>Returns full details for a single event including its categories.</summary>
    /// <response code="200">Event detail object.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null) return NotFound(new { status = 404, error = "Not Found", message = "Event not found." });
        return Ok(ToResponse(ev, includeCategories: true));
    }

    /// <summary>Creates a new event. The authenticated Organiser is set as the owner.</summary>
    /// <response code="201">New event created.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Requires Organiser role.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest req)
    {
        var ev = new Event
        {
            OrganiserID = CurrentUserId,
            Name = req.Name,
            Description = req.Description,
            EventDate = req.EventDate,
            Location = req.Location,
            City = req.City,
            Province = req.Province,
            ImageURL = req.ImageURL
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();

        await db.Entry(ev).Reference(e => e.Organiser).LoadAsync();
        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, ToResponse(ev));
    }

    /// <summary>Updates an existing event. Only the owning Organiser may update.</summary>
    /// <response code="200">Updated event object.</response>
    /// <response code="403">Not the event owner.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventRequest req)
    {
        var ev = await db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null) return NotFound(new { status = 404, error = "Not Found", message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId)
            return Forbid();

        if (req.Name != null) ev.Name = req.Name;
        if (req.Description != null) ev.Description = req.Description;
        if (req.EventDate.HasValue) ev.EventDate = req.EventDate.Value;
        if (req.Location != null) ev.Location = req.Location;
        if (req.City != null) ev.City = req.City;
        if (req.Province != null) ev.Province = req.Province;
        if (req.Status != null) ev.Status = req.Status;
        if (req.ImageURL != null) ev.ImageURL = req.ImageURL;

        await db.SaveChangesAsync();
        return Ok(ToResponse(ev));
    }

    /// <summary>Deletes an event and all its categories. Only the owning Organiser may delete.</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="403">Not the event owner.</response>
    /// <response code="404">Event not found.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { status = 404, error = "Not Found", message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId)
            return Forbid();

        db.Events.Remove(ev);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Returns all enrolments for an event. Only the event's Organiser may view.</summary>
    /// <response code="200">Array of enrolment objects with participant details.</response>
    /// <response code="403">Not the event owner.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventEnrolments(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { status = 404, error = "Not Found", message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId)
            return Forbid();

        var enrolments = await db.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Category)
                .ThenInclude(c => c.Event)
            .Where(en => en.Category.EventID == id)
            .ToListAsync();

        return Ok(enrolments.Select(en => new EnrolmentResponse(
            en.EnrolmentID, en.ParticipantID, en.Participant.FullName,
            en.CategoryID, en.Category.Name, en.Category.EventID, en.Category.Event.Name,
            en.EnrolmentDate, en.PaymentStatus, en.BibNumber)));
    }

    /// <summary>Returns published results for an event. Public access.</summary>
    /// <response code="200">Array of result objects.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}/results")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventResults(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { status = 404, error = "Not Found", message = "Event not found." });

        var results = await db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.Participant)
            .Include(r => r.Enrolment).ThenInclude(en => en.Category).ThenInclude(c => c.Event)
            .Where(r => r.Enrolment.Category.EventID == id)
            .ToListAsync();

        return Ok(results.Select(r => new ResultResponse(
            r.ResultID, r.EnrolmentID, r.Enrolment.Participant.FullName,
            r.Enrolment.Category.Event.Name, r.Enrolment.Category.Name,
            r.FinishTime, r.Position, r.Status, r.Notes, r.RecordedAt)));
    }
}
