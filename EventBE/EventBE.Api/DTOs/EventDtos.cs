using System.ComponentModel.DataAnnotations;

namespace EventBE.Api.DTOs;

public record CreateEventRequest
{
    [Required(ErrorMessage = "Event title is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 150 characters.")]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string Description { get; init; } = string.Empty;

    [Required(ErrorMessage = "Event date is required.")]
    public DateTime Date { get; init; }

    [Range(1, 100000, ErrorMessage = "Max capacity must be at least 1.")]
    public int MaxCapacity { get; init; }
}

public record UpdateEventRequest
{
    [Required(ErrorMessage = "Event title is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 150 characters.")]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string Description { get; init; } = string.Empty;

    [Required(ErrorMessage = "Event date is required.")]
    public DateTime Date { get; init; }

    [Range(1, 100000, ErrorMessage = "Max capacity must be at least 1.")]
    public int MaxCapacity { get; init; }
}

public record RegisterAttendeeRequest
{
    [Required(ErrorMessage = "User name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "User name must be between 1 and 100 characters.")]
    public string UserName { get; init; } = string.Empty;

    [Required(ErrorMessage = "User email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    public string UserEmail { get; init; } = string.Empty;
}

public record RegistrationDto(
    Guid Id,
    Guid EventId,
    string UserName,
    string UserEmail,
    DateTime RegisteredAt
);

public record EventDto(
    Guid Id,
    string Title,
    string Description,
    DateTime Date,
    int MaxCapacity,
    int RegisteredCount,
    int AvailableSpots,
    bool IsPast,
    bool IsFull,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<RegistrationDto> Registrations
);

public record ApiErrorResponse(
    string Message,
    string ErrorType
);

