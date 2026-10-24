using System.Net.Mail;
using EventBE.Api.Common;
using EventBE.Api.DTOs;
using EventBE.Api.Models;

namespace EventBE.Api.Services;

/// <summary>
/// Thread-safe in-memory event and registration management service enforcing all business rules.
/// </summary>
public class EventService : IEventService
{
    private readonly Dictionary<Guid, EventItem> _events = new();
    private readonly object _syncRoot = new();
    private readonly TimeProvider _timeProvider;

    public EventService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IReadOnlyList<EventDto> GetAllEvents()
    {
        lock (_syncRoot)
        {
            return _events.Values
                .OrderBy(e => e.Date)
                .Select(MapToDto)
                .ToList();
        }
    }

    public ServiceResult<EventDto> GetEventById(Guid id)
    {
        lock (_syncRoot)
        {
            if (!_events.TryGetValue(id, out var eventItem))
            {
                return ServiceResult<EventDto>.Failure(
                    $"Event with ID '{id}' was not found.",
                    ServiceErrorType.NotFound);
            }

            return ServiceResult<EventDto>.Success(MapToDto(eventItem));
        }
    }

    public ServiceResult<EventDto> CreateEvent(CreateEventRequest request)
    {
        var validationError = ValidateEventInput(request.Title, request.MaxCapacity);
        if (validationError is not null)
        {
            return ServiceResult<EventDto>.Failure(validationError, ServiceErrorType.ValidationError);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var normalizedDate = NormalizeToUtc(request.Date);

        var newEvent = new EventItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Date = normalizedDate,
            MaxCapacity = request.MaxCapacity,
            CreatedAt = now,
            UpdatedAt = now
        };

        lock (_syncRoot)
        {
            _events[newEvent.Id] = newEvent;
            return ServiceResult<EventDto>.Success(MapToDto(newEvent));
        }
    }

    public ServiceResult<EventDto> UpdateEvent(Guid id, UpdateEventRequest request)
    {
        var validationError = ValidateEventInput(request.Title, request.MaxCapacity);
        if (validationError is not null)
        {
            return ServiceResult<EventDto>.Failure(validationError, ServiceErrorType.ValidationError);
        }

        lock (_syncRoot)
        {
            if (!_events.TryGetValue(id, out var existingEvent))
            {
                return ServiceResult<EventDto>.Failure(
                    $"Event with ID '{id}' was not found.",
                    ServiceErrorType.NotFound);
            }

            if (request.MaxCapacity < existingEvent.Registrations.Count)
            {
                return ServiceResult<EventDto>.Failure(
                    $"Cannot reduce max capacity to {request.MaxCapacity} because {existingEvent.Registrations.Count} attendees are already registered.",
                    ServiceErrorType.ValidationError);
            }

            existingEvent.Title = request.Title.Trim();
            existingEvent.Description = request.Description?.Trim() ?? string.Empty;
            existingEvent.Date = NormalizeToUtc(request.Date);
            existingEvent.MaxCapacity = request.MaxCapacity;
            existingEvent.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

            return ServiceResult<EventDto>.Success(MapToDto(existingEvent));
        }
    }

    public ServiceResult<bool> DeleteEvent(Guid id)
    {
        lock (_syncRoot)
        {
            if (!_events.Remove(id))
            {
                return ServiceResult<bool>.Failure(
                    $"Event with ID '{id}' was not found.",
                    ServiceErrorType.NotFound);
            }

            return ServiceResult<bool>.Success(true);
        }
    }

    public ServiceResult<RegistrationDto> RegisterAttendee(Guid eventId, RegisterAttendeeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return ServiceResult<RegistrationDto>.Failure(
                "Attendee name is required.",
                ServiceErrorType.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(request.UserEmail) || !IsValidEmail(request.UserEmail))
        {
            return ServiceResult<RegistrationDto>.Failure(
                "A valid attendee email address is required.",
                ServiceErrorType.ValidationError);
        }

        var normalizedEmail = request.UserEmail.Trim().ToLowerInvariant();
        var trimmedName = request.UserName.Trim();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        lock (_syncRoot)
        {
            if (!_events.TryGetValue(eventId, out var eventItem))
            {
                return ServiceResult<RegistrationDto>.Failure(
                    $"Event with ID '{eventId}' was not found.",
                    ServiceErrorType.NotFound);
            }

            // Business Rule 1: Cannot register for past events
            if (eventItem.Date <= now)
            {
                return ServiceResult<RegistrationDto>.Failure(
                    "Cannot register for past events.",
                    ServiceErrorType.ValidationError);
            }

            // Business Rule 3: Cannot double-register same user for same event
            var alreadyRegistered = eventItem.Registrations.Any(r =>
                string.Equals(r.UserEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase));

            if (alreadyRegistered)
            {
                return ServiceResult<RegistrationDto>.Failure(
                    $"User '{normalizedEmail}' is already registered for this event.",
                    ServiceErrorType.Conflict);
            }

            // Business Rule 2: Cannot exceed event capacity
            if (eventItem.Registrations.Count >= eventItem.MaxCapacity)
            {
                return ServiceResult<RegistrationDto>.Failure(
                    "Cannot exceed event capacity. This event is already full.",
                    ServiceErrorType.Conflict);
            }

            var registration = new Registration
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                UserName = trimmedName,
                UserEmail = normalizedEmail,
                RegisteredAt = now
            };

            eventItem.Registrations.Add(registration);
            eventItem.UpdatedAt = now;

            return ServiceResult<RegistrationDto>.Success(MapRegistrationToDto(registration));
        }
    }

    public ServiceResult<bool> UnregisterAttendee(Guid eventId, string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return ServiceResult<bool>.Failure(
                "Attendee email is required to unregister.",
                ServiceErrorType.ValidationError);
        }

        var normalizedEmail = userEmail.Trim();

        lock (_syncRoot)
        {
            if (!_events.TryGetValue(eventId, out var eventItem))
            {
                return ServiceResult<bool>.Failure(
                    $"Event with ID '{eventId}' was not found.",
                    ServiceErrorType.NotFound);
            }

            var registration = eventItem.Registrations.FirstOrDefault(r =>
                string.Equals(r.UserEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase));

            if (registration is null)
            {
                return ServiceResult<bool>.Failure(
                    $"User '{normalizedEmail}' is not registered for this event.",
                    ServiceErrorType.NotFound);
            }

            eventItem.Registrations.Remove(registration);
            eventItem.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

            return ServiceResult<bool>.Success(true);
        }
    }

    public void SeedSampleData()
    {
        lock (_syncRoot)
        {
            if (_events.Count > 0)
            {
                return;
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var cloudSummitId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var cloudSummit = new EventItem
            {
                Id = cloudSummitId,
                Title = "Cloud Architecture & .NET 8 Summit",
                Description = "Deep dive into scalable distributed systems, minimal APIs, and modern cloud-native patterns with live Q&A.",
                Date = now.AddDays(14),
                MaxCapacity = 25,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-1),
                Registrations = new List<Registration>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EventId = cloudSummitId,
                        UserName = "Alice Rivera",
                        UserEmail = "alice@origin.dev",
                        RegisteredAt = now.AddDays(-3)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EventId = cloudSummitId,
                        UserName = "Marcus Vance",
                        UserEmail = "marcus@origin.dev",
                        RegisteredAt = now.AddDays(-2)
                    }
                }
            };

            var workshopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var workshop = new EventItem
            {
                Id = workshopId,
                Title = "Executive AI Strategy Roundtable (Intimate Session)",
                Description = "Hands-on executive roundtable capped at 2 participants to demonstrate capacity limits. Currently at full capacity.",
                Date = now.AddDays(5),
                MaxCapacity = 2,
                CreatedAt = now.AddDays(-4),
                UpdatedAt = now.AddDays(-1),
                Registrations = new List<Registration>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EventId = workshopId,
                        UserName = "Elena Rostova",
                        UserEmail = "elena@origin.dev",
                        RegisteredAt = now.AddDays(-2)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EventId = workshopId,
                        UserName = "Devon Brooks",
                        UserEmail = "devon@origin.dev",
                        RegisteredAt = now.AddDays(-1)
                    }
                }
            };

            var reactClinicId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var reactClinic = new EventItem
            {
                Id = reactClinicId,
                Title = "React + Vite Performance Engineering Clinic",
                Description = "Interactive workshop covering state management, bundle optimization, and responsive UI design patterns.",
                Date = now.AddDays(21),
                MaxCapacity = 10,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2),
                Registrations = new List<Registration>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EventId = reactClinicId,
                        UserName = "Sophia Chen",
                        UserEmail = "sophia@origin.dev",
                        RegisteredAt = now.AddDays(-1)
                    }
                }
            };

            var pastEventId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var pastEvent = new EventItem
            {
                Id = pastEventId,
                Title = "Q3 Engineering Retrospective & Demo Day",
                Description = "Completed engineering showcase from last month. New registrations are closed because this event is in the past.",
                Date = now.AddDays(-10),
                MaxCapacity = 15,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-10),
                Registrations = new List<Registration>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EventId = pastEventId,
                        UserName = "Jordan Patel",
                        UserEmail = "jordan@origin.dev",
                        RegisteredAt = now.AddDays(-15)
                    }
                }
            };

            _events[cloudSummit.Id] = cloudSummit;
            _events[workshop.Id] = workshop;
            _events[reactClinic.Id] = reactClinic;
            _events[pastEvent.Id] = pastEvent;
        }
    }

    private EventDto MapToDto(EventItem item)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var registeredCount = item.Registrations.Count;
        var availableSpots = Math.Max(0, item.MaxCapacity - registeredCount);
        var isPast = item.Date <= now;
        var isFull = registeredCount >= item.MaxCapacity;

        var registrations = item.Registrations
            .OrderByDescending(r => r.RegisteredAt)
            .Select(MapRegistrationToDto)
            .ToList();

        return new EventDto(
            item.Id,
            item.Title,
            item.Description,
            item.Date,
            item.MaxCapacity,
            registeredCount,
            availableSpots,
            isPast,
            isFull,
            item.CreatedAt,
            item.UpdatedAt,
            registrations
        );
    }

    private static RegistrationDto MapRegistrationToDto(Registration r) =>
        new(r.Id, r.EventId, r.UserName, r.UserEmail, r.RegisteredAt);

    private static string? ValidateEventInput(string? title, int maxCapacity)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 2)
        {
            return "Event title is required and must be at least 2 characters.";
        }

        if (maxCapacity < 1)
        {
            return "Max capacity must be at least 1.";
        }

        return null;
    }

    private static DateTime NormalizeToUtc(DateTime date)
    {
        return date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email.Trim());
            return addr.Address == email.Trim() && email.Contains('.');
        }
        catch
        {
            return false;
        }
    }
}

