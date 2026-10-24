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
    public void CreateEvent_WithValidInput_ReturnsCreatedEvent()
    {
        // Arrange
        var service = CreateService();
        var request = new CreateEventRequest
        {
            Title = "Architecture Design Review",
            Description = "Quarterly platform review session",
            Date = _fixedNow.UtcDateTime.AddDays(7),
            MaxCapacity = 20
        };

        // Act
        var result = service.CreateEvent(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal("Architecture Design Review", result.Value.Title);
        Assert.Equal(20, result.Value.MaxCapacity);
        Assert.Equal(0, result.Value.RegisteredCount);
        Assert.Equal(20, result.Value.AvailableSpots);
        Assert.False(result.Value.IsPast);
        Assert.False(result.Value.IsFull);
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("A", 10)]
    [InlineData("Valid Event", 0)]
    [InlineData("Valid Event", -5)]
    public void CreateEvent_WithInvalidTitleOrCapacity_ReturnsValidationError(string title, int capacity)
    {
        // Arrange
        var service = CreateService();
        var request = new CreateEventRequest
        {
            Title = title,
            Description = "Invalid event test",
            Date = _fixedNow.UtcDateTime.AddDays(3),
            MaxCapacity = capacity
        };

        // Act
        var result = service.CreateEvent(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.ValidationError, result.ErrorType);
    }

    [Fact]
    public void UpdateEvent_WithValidChanges_UpdatesEventProperties()
    {
        // Arrange
        var service = CreateService();
        var created = service.CreateEvent(new CreateEventRequest
        {
            Title = "Initial Title",
            Description = "Initial Desc",
            Date = _fixedNow.UtcDateTime.AddDays(5),
            MaxCapacity = 10
        }).Value!;

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title",
            Description = "Updated Desc",
            Date = _fixedNow.UtcDateTime.AddDays(10),
            MaxCapacity = 15
        };

        // Act
        var updateResult = service.UpdateEvent(created.Id, updateRequest);

        // Assert
        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Updated Title", updateResult.Value!.Title);
        Assert.Equal("Updated Desc", updateResult.Value.Description);
        Assert.Equal(15, updateResult.Value.MaxCapacity);
    }

    [Fact]
    public void UpdateEvent_ReducingCapacityBelowRegisteredCount_ReturnsValidationError()
    {
        // Arrange
        var service = CreateService();
        var created = service.CreateEvent(new CreateEventRequest
        {
            Title = "Workshop",
            Description = "Hands-on coding",
            Date = _fixedNow.UtcDateTime.AddDays(5),
            MaxCapacity = 5
        }).Value!;

        service.RegisterAttendee(created.Id, new RegisterAttendeeRequest { UserName = "User 1", UserEmail = "u1@test.com" });
        service.RegisterAttendee(created.Id, new RegisterAttendeeRequest { UserName = "User 2", UserEmail = "u2@test.com" });

        // Act: Attempt to reduce capacity to 1 when 2 users are registered
        var updateResult = service.UpdateEvent(created.Id, new UpdateEventRequest
        {
            Title = "Workshop",
            Description = "Hands-on coding",
            Date = _fixedNow.UtcDateTime.AddDays(5),
            MaxCapacity = 1
        });

        // Assert
        Assert.False(updateResult.IsSuccess);
        Assert.Equal(ServiceErrorType.ValidationError, updateResult.ErrorType);
        Assert.Contains("already registered", updateResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
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

        var currentEvent = service.GetEventById(limitedEvent.Id).Value!;
        Assert.True(currentEvent.IsFull);
        Assert.Equal(0, currentEvent.AvailableSpots);
        Assert.Equal(2, currentEvent.RegisteredCount);
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

    [Fact]
    public void RegisterAttendee_SameUserForDifferentEvents_Succeeds()
    {
        // Arrange
        var service = CreateService();
        var eventA = service.CreateEvent(new CreateEventRequest
        {
            Title = "Event A",
            Description = "First event",
            Date = _fixedNow.UtcDateTime.AddDays(2),
            MaxCapacity = 10
        }).Value!;
        var eventB = service.CreateEvent(new CreateEventRequest
        {
            Title = "Event B",
            Description = "Second event",
            Date = _fixedNow.UtcDateTime.AddDays(4),
            MaxCapacity = 10
        }).Value!;

        // Act
        var regA = service.RegisterAttendee(eventA.Id, new RegisterAttendeeRequest
        {
            UserName = "Sam Taylor",
            UserEmail = "sam@example.com"
        });
        var regB = service.RegisterAttendee(eventB.Id, new RegisterAttendeeRequest
        {
            UserName = "Sam Taylor",
            UserEmail = "sam@example.com"
        });

        // Assert
        Assert.True(regA.IsSuccess);
        Assert.True(regB.IsSuccess);
    }

    [Fact]
    public void UnregisterAttendee_FreesCapacityAndAllowsNewRegistration()
    {
        // Arrange
        var service = CreateService();
        var soloEvent = service.CreateEvent(new CreateEventRequest
        {
            Title = "1-on-1 Coaching",
            Description = "Single slot session",
            Date = _fixedNow.UtcDateTime.AddDays(2),
            MaxCapacity = 1
        }).Value!;

        service.RegisterAttendee(soloEvent.Id, new RegisterAttendeeRequest
        {
            UserName = "First User",
            UserEmail = "first@example.com"
        });

        // Act — Unregister first user, then register second user
        var unregisterResult = service.UnregisterAttendee(soloEvent.Id, "FIRST@example.com");
        var secondRegResult = service.RegisterAttendee(soloEvent.Id, new RegisterAttendeeRequest
        {
            UserName = "Second User",
            UserEmail = "second@example.com"
        });

        // Assert
        Assert.True(unregisterResult.IsSuccess);
        Assert.True(secondRegResult.IsSuccess);

        var updatedEvent = service.GetEventById(soloEvent.Id).Value!;
        Assert.Equal(1, updatedEvent.RegisteredCount);
        Assert.Equal("second@example.com", updatedEvent.Registrations[0].UserEmail);
    }

    [Fact]
    public void UnregisterAttendee_WhenUserNotRegistered_ReturnsNotFound()
    {
        // Arrange
        var service = CreateService();
        var upcomingEvent = service.CreateEvent(new CreateEventRequest
        {
            Title = "Town Hall",
            Description = "Q&A",
            Date = _fixedNow.UtcDateTime.AddDays(5),
            MaxCapacity = 50
        }).Value!;

        // Act
        var result = service.UnregisterAttendee(upcomingEvent.Id, "nobody@example.com");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
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

