using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public record UserProfileResponse(
    int UserID,
    string FullName,
    string Email,
    string Role,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    DateTime CreatedAt
);

public record UpdateProfileRequest(
    [MaxLength(100)] string? FullName,
    [MaxLength(20)] string? PhoneNumber,
    DateOnly? DateOfBirth
);
