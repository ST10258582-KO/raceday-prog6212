using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>
/// Manages race categories within events — age groups, distances, and event types.
/// </summary>
[ApiController]
[Route("api/events/{eventId:int}/categories")]
[Produces("application/json")]
public class CategoriesController(RaceDayContext db) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static CategoryResponse ToResponse(Category c) =>
        new(c.CategoryID, c.EventID, c.Name, c.Distance, c.DistanceUnit,
            c.EntryFee, c.MaxParticipants, c.EventType, c.CreatedAt);

    /// <summary>Returns all race categories for a given event. Public access.</summary>
    /// <response code="200">Array of category objects.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new { status = 404, error = "Not Found", message = "Event not found." });

        var categories = await db.Categories
            .Where(c => c.EventID == eventId)
            .ToListAsync();

        return Ok(categories.Select(ToResponse));
    }

    /// <summary>Returns details for a single category within an event. Public access.</summary>
    /// <response code="200">Category object.</response>
    /// <response code="404">Event or category not found.</response>
    [HttpGet("{categoryId:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(int eventId, int categoryId)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.EventID == eventId && c.CategoryID == categoryId);
        if (category == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Category not found." });

        return Ok(ToResponse(category));
    }

    /// <summary>Adds a new race category to an event. Only the event's Organiser may add categories.</summary>
    /// <response code="201">New category created.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="403">Not the event owner.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CreateCategoryRequest req)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId)
            return Forbid();

        var validTypes = new[] { "Running", "Walking", "Cycling" };
        if (!validTypes.Contains(req.EventType))
            return BadRequest(new { status = 400, error = "Bad Request", message = "EventType must be 'Running', 'Walking', or 'Cycling'." });

        var category = new Category
        {
            EventID = eventId,
            Name = req.Name,
            Distance = req.Distance,
            DistanceUnit = req.DistanceUnit ?? "km",
            EntryFee = req.EntryFee,
            MaxParticipants = req.MaxParticipants,
            EventType = req.EventType
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { eventId, categoryId = category.CategoryID }, ToResponse(category));
    }

    /// <summary>Updates an existing category. Only the event's Organiser may update.</summary>
    /// <response code="200">Updated category object.</response>
    /// <response code="403">Not the event owner.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int eventId, int categoryId, [FromBody] UpdateCategoryRequest req)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId)
            return Forbid();

        var category = await db.Categories.FirstOrDefaultAsync(c => c.EventID == eventId && c.CategoryID == categoryId);
        if (category == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Category not found." });

        if (req.Name != null) category.Name = req.Name;
        if (req.Distance.HasValue) category.Distance = req.Distance.Value;
        if (req.DistanceUnit != null) category.DistanceUnit = req.DistanceUnit;
        if (req.EntryFee.HasValue) category.EntryFee = req.EntryFee.Value;
        if (req.MaxParticipants.HasValue) category.MaxParticipants = req.MaxParticipants;
        if (req.EventType != null) category.EventType = req.EventType;

        await db.SaveChangesAsync();
        return Ok(ToResponse(category));
    }

    /// <summary>Deletes a category from an event. Only the event's Organiser may delete.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="403">Not the event owner.</response>
    /// <response code="404">Category not found.</response>
    [HttpDelete("{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId)
            return Forbid();

        var category = await db.Categories.FirstOrDefaultAsync(c => c.EventID == eventId && c.CategoryID == categoryId);
        if (category == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Category not found." });

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
