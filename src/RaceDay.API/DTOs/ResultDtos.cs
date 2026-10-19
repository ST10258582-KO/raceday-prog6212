using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public record ResultResponse(
    int ResultID,
    int EnrolmentID,
    string ParticipantName,
    string EventName,
    string CategoryName,
    TimeOnly? FinishTime,
    int? Position,
    string Status,
    string? Notes,
    DateTime RecordedAt
);

public record CreateResultRequest(
    [Required] int EnrolmentID,
    TimeOnly? FinishTime,
    int? Position,
    [Required] string Status,
    [MaxLength(500)] string? Notes
);

public record UpdateResultRequest(
    TimeOnly? FinishTime,
    int? Position,
    string? Status,
    [MaxLength(500)] string? Notes
);
