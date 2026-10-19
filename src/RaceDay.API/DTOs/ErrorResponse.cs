namespace RaceDay.API.DTOs;

/// <summary>
/// Standard error envelope returned by all API error responses.
/// </summary>
public record ErrorResponse(int Status, string Error, string Message);
