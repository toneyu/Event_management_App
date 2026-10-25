using EventBE.Api.Common;
using EventBE.Api.DTOs;
using EventBE.Api.Services;

namespace EventBE.Tests;

public class EventServiceTests
{
    private readonly DateTimeOffset _fixedNow = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    private EventService CreateService(DateTimeOffset? currentTime = null)
    {
        var clock = new FakeTimeProvider(currentTime ?? _fixedNow);
        return new EventService(clock);
    }

    [Fact]
    public void RegisterAttendee_ForPastEvent_FailsWithValidationError()
    {
        // Arrange — Business Rule 1: Cannot register for past events
        var service = CreateService();
        var pastEvent = service.CreateEvent(new CreateEventRequest
        {
            Title = "Last Week Meetup",
            Description = "Already occurred",
            Date = _fixedNow.UtcDateTime.AddDays(-1),
            MaxCapacity = 50
        }).Value!;

        // Act
        var result = service.RegisterAttendee(pastEvent.Id, new RegisterAttendeeRequest
        {
            UserName = "Jane Doe",
            UserEmail = "jane@example.com"
        });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.ValidationError, result.ErrorType);
        Assert.Contains("past events", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegisterAttendee_WhenEventAtMaxCapacity_FailsWithConflict()
    {
        // Arrange — Business Rule 2: Cannot exceed event capacity
        var service = CreateService();
        var limitedEvent = service.CreateEvent(new CreateEventRequest
        {
            Title = "VIP Session",
            Description = "Strictly limited to 2 seats",
            Date = _fixedNow.UtcDateTime.AddDays(3),
            MaxCapacity = 2
        }).Value!;

        var reg1 = service.RegisterAttendee(limitedEvent.Id, new RegisterAttendeeRequest
        {
            UserName = "Attendee One",
            UserEmail = "one@example.com"
        });
        var reg2 = service.RegisterAttendee(limitedEvent.Id, new RegisterAttendeeRequest
        {
            UserName = "Attendee Two",
            UserEmail = "two@example.com"
        });

        // Act — Attempt 3rd registration on a 2-capacity event
        var reg3 = service.RegisterAttendee(limitedEvent.Id, new RegisterAttendeeRequest
        {
            UserName = "Attendee Three",
            UserEmail = "three@example.com"
        });

        // Assert
        Assert.True(reg1.IsSuccess);
        Assert.True(reg2.IsSuccess);
        Assert.False(reg3.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, reg3.ErrorType);
        Assert.Contains("capacity", reg3.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegisterAttendee_DuplicateUserForSameEvent_FailsWithConflict()
    {
        // Arrange — Business Rule 3: Cannot double-register same user for same event
        var service = CreateService();
        var upcomingEvent = service.CreateEvent(new CreateEventRequest
        {
            Title = "Product Launch",
            Description = "Annual keynote",
            Date = _fixedNow.UtcDateTime.AddDays(10),
            MaxCapacity = 100
        }).Value!;

        var firstAttempt = service.RegisterAttendee(upcomingEvent.Id, new RegisterAttendeeRequest
        {
            UserName = "Alex Johnson",
            UserEmail = "alex.johnson@example.com"
        });

        // Act — Attempt duplicate registration with different casing & whitespace
        var duplicateAttempt = service.RegisterAttendee(upcomingEvent.Id, new RegisterAttendeeRequest
        {
            UserName = "Alex J.",
            UserEmail = "  ALEX.JOHNSON@EXAMPLE.COM "
        });

        // Assert
        Assert.True(firstAttempt.IsSuccess);
        Assert.False(duplicateAttempt.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, duplicateAttempt.ErrorType);
        Assert.Contains("already registered", duplicateAttempt.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
