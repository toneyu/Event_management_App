using EventBE.Api.Common;
using EventBE.Api.DTOs;

namespace EventBE.Api.Services;

public interface IEventService
{
    IReadOnlyList<EventDto> GetAllEvents();
    ServiceResult<EventDto> GetEventById(Guid id);
    ServiceResult<EventDto> CreateEvent(CreateEventRequest request);
    ServiceResult<EventDto> UpdateEvent(Guid id, UpdateEventRequest request);
    ServiceResult<bool> DeleteEvent(Guid id);
    ServiceResult<RegistrationDto> RegisterAttendee(Guid eventId, RegisterAttendeeRequest request);
    ServiceResult<bool> UnregisterAttendee(Guid eventId, string userEmail);
    void SeedSampleData();
}

