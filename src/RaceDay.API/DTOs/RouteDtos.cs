using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public record RouteResponse(
    int RouteID,
    int EventID,
    string? RouteDescription,
    string? MapURL,
    string? GPXData,
    decimal? ElevationGain,
    DateTime CreatedAt
);

public record CreateRouteRequest(
    string? RouteDescription,
    [MaxLength(500)] string? MapURL,
    string? GPXData,
    decimal? ElevationGain
);

public record UpdateRouteRequest(
    string? RouteDescription,
    [MaxLength(500)] string? MapURL,
    string? GPXData,
    decimal? ElevationGain
);
