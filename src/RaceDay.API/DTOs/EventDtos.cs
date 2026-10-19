using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public record EventResponse(
    int EventID,
    int OrganiserID,
    string OrganiserName,
    string Name,
    string? Description,
    DateOnly EventDate,
    string Location,
    string City,
    string Province,
    string Status,
    string? ImageURL,
    DateTime CreatedAt,
    IEnumerable<CategoryResponse>? Categories
);

public record CreateEventRequest(
    [Required, MaxLength(150)] string Name,
    string? Description,
    [Required] DateOnly EventDate,
    [Required, MaxLength(200)] string Location,
    [Required, MaxLength(100)] string City,
    [Required, MaxLength(100)] string Province,
    [MaxLength(500)] string? ImageURL
);

public record UpdateEventRequest(
    [MaxLength(150)] string? Name,
    string? Description,
    DateOnly? EventDate,
    [MaxLength(200)] string? Location,
    [MaxLength(100)] string? City,
    [MaxLength(100)] string? Province,
    [MaxLength(20)] string? Status,
    [MaxLength(500)] string? ImageURL
);
