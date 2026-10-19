using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public record RegisterRequest(
    [Required, MaxLength(100)] string FullName,
    [Required, EmailAddress, MaxLength(150)] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string Role,
    string? PhoneNumber,
    DateOnly? DateOfBirth
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(
    int UserID,
    string FullName,
    string Email,
    string Role,
    string Token
);
