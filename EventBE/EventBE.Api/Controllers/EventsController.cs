using EventBE.Api.Common;
using EventBE.Api.DTOs;
using EventBE.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventBE.Api.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Retrieves all events ordered by date.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<EventDto>> GetAllEvents()
    {
        var events = _eventService.GetAllEvents();
        return Ok(events);
    }

    /// <summary>
    /// Retrieves a single event and its registrations by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EventDto> GetEventById(Guid id)
    {
        var result = _eventService.GetEventById(id);
        return ToActionResult(result);
    }

    /// <summary>
    /// Creates a new event.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<EventDto> CreateEvent([FromBody] CreateEventRequest request)
    {
        var result = _eventService.CreateEvent(request);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result);
        }

        return CreatedAtAction(nameof(GetEventById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Updates an existing event's details (title, description, date, max capacity).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EventDto> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
    {
        var result = _eventService.UpdateEvent(id, request);
        return ToActionResult(result);
    }

    /// <summary>
    /// Deletes an event by ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult DeleteEvent(Guid id)
    {
        var result = _eventService.DeleteEvent(id);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result);
        }

        return NoContent();
    }

    /// <summary>
    /// Registers an attendee for an event, enforcing capacity, duplicate, and past-date rules.
    /// </summary>
    [HttpPost("{id:guid}/registrations")]
    [ProducesResponseType(typeof(RegistrationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public ActionResult<RegistrationDto> RegisterAttendee(Guid id, [FromBody] RegisterAttendeeRequest request)
    {
        var result = _eventService.RegisterAttendee(id, request);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Unregisters an attendee from an event by email address.
    /// </summary>
    [HttpDelete("{id:guid}/registrations/{userEmail}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult UnregisterAttendee(Guid id, string userEmail)
    {
        var decodedEmail = Uri.UnescapeDataString(userEmail);
        var result = _eventService.UnregisterAttendee(id, decodedEmail);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result);
        }

        return NoContent();
    }

    private ActionResult<T> ToActionResult<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return ToErrorResult(result);
    }

    private ObjectResult ToErrorResult<T>(ServiceResult<T> result)
    {
        var errorPayload = new ApiErrorResponse(
            result.ErrorMessage ?? "An unexpected error occurred.",
            result.ErrorType.ToString()
        );

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => NotFound(errorPayload),
            ServiceErrorType.Conflict => Conflict(errorPayload),
            _ => BadRequest(errorPayload)
        };
    }
}

