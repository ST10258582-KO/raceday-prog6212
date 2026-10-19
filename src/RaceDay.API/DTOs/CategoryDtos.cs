using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public record CategoryResponse(
    int CategoryID,
    int EventID,
    string Name,
    decimal Distance,
    string DistanceUnit,
    decimal EntryFee,
    int? MaxParticipants,
    string EventType,
    DateTime CreatedAt
);

public record CreateCategoryRequest(
    [Required, MaxLength(100)] string Name,
    [Required, Range(0.01, double.MaxValue)] decimal Distance,
    string? DistanceUnit,
    [Range(0, double.MaxValue)] decimal EntryFee,
    int? MaxParticipants,
    [Required] string EventType
);

public record UpdateCategoryRequest(
    [MaxLength(100)] string? Name,
    [Range(0.01, double.MaxValue)] decimal? Distance,
    string? DistanceUnit,
    [Range(0, double.MaxValue)] decimal? EntryFee,
    int? MaxParticipants,
    string? EventType
);
