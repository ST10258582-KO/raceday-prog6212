using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public record EnrolmentResponse(
    int EnrolmentID,
    int ParticipantID,
    string ParticipantName,
    int CategoryID,
    string CategoryName,
    int EventID,
    string EventName,
    DateTime EnrolmentDate,
    string PaymentStatus,
    string? BibNumber
);

public record CreateEnrolmentRequest(
    [Required] int CategoryID
);
