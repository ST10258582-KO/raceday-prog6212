using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>
/// Manages GPS route and map information for events.
/// </summary>
[ApiController]
[Route("api/events/{eventId:int}/route")]
[Produces("application/json")]
public class RoutesController(RaceDayContext db) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static RouteResponse ToResponse(EventRoute r) =>
        new(r.RouteID, r.EventID, r.RouteDescription, r.MapURL, r.GPXData, r.ElevationGain, r.CreatedAt);

    /// <summary>Returns route and map information for an event. Public access.</summary>
    /// <response code="200">Route object.</response>
    /// <response code="404">Event or route not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoute(int eventId)
    {
        var route = await db.EventRoutes.FirstOrDefaultAsync(r => r.EventID == eventId);
        if (route == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Route not found for this event." });
        return Ok(ToResponse(route));
    }

    /// <summary>Adds route information to an event. Only the event's Organiser may add a route.</summary>
    /// <response code="201">Route created.</response>
    /// <response code="400">Route already exists — use PUT to update.</response>
    /// <response code="403">Not the event owner.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRoute(int eventId, [FromBody] CreateRouteRequest req)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId)
            return Forbid();

        if (await db.EventRoutes.AnyAsync(r => r.EventID == eventId))
            return BadRequest(new { status = 400, error = "Bad Request", message = "A route already exists for this event. Use PUT to update." });

        var route = new EventRoute
        {
            EventID = eventId,
            RouteDescription = req.RouteDescription,
            MapURL = req.MapURL,
            GPXData = req.GPXData,
            ElevationGain = req.ElevationGain
        };

        db.EventRoutes.Add(route);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRoute), new { eventId }, ToResponse(route));
    }

    /// <summary>Updates route information for an event. Only the event's Organiser may update.</summary>
    /// <response code="200">Updated route object.</response>
    /// <response code="403">Not the event owner.</response>
    /// <response code="404">Route not found.</response>
    [HttpPut]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoute(int eventId, [FromBody] UpdateRouteRequest req)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId)
            return Forbid();

        var route = await db.EventRoutes.FirstOrDefaultAsync(r => r.EventID == eventId);
        if (route == null)
            return NotFound(new { status = 404, error = "Not Found", message = "Route not found. Use POST to create one." });

        if (req.RouteDescription != null) route.RouteDescription = req.RouteDescription;
        if (req.MapURL != null) route.MapURL = req.MapURL;
        if (req.GPXData != null) route.GPXData = req.GPXData;
        if (req.ElevationGain.HasValue) route.ElevationGain = req.ElevationGain;

        await db.SaveChangesAsync();
        return Ok(ToResponse(route));
    }
}
