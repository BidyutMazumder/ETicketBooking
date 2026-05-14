namespace Booking.API.Controllers;

/// <summary>
/// Event Management Endpoints
/// Handles creation and management of events
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class EventsController : BaseController
{
    public EventsController(ISender sender) : base(sender) { }

    /// <summary>
    /// Create a new event
    /// </summary>
    /// <remarks>
    /// Creates a new event in the system. The event will be in unpublished state initially.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/events/create
    ///     {
    ///        "title": "Taylor Swift - Eras Tour",
    ///        "description": "The biggest concert of the year",
    ///        "startDateTime": "2026-06-15T20:00:00Z",
    ///        "venueName": "MetLife Stadium"
    ///     }
    /// </remarks>
    /// <param name="command">Event creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created event with assigned ID</returns>
    /// <response code="201">Event created successfully</response>
    /// <response code="400">Invalid input or validation error</response>
    /// <response code="500">Server error</response>
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEventCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess 
            ? CreatedAtAction(nameof(Create), new { id = result.Data?.Id }, result)
            : BadRequest(result);
    }

    /// <summary>
    /// Get available seats for an event
    /// </summary>
    /// <remarks>
    /// Retrieves all available seats (not held, reserved, or sold) for a specific event.
    /// This list changes in real-time as users place holds and make reservations.
    /// </remarks>
    /// <param name="eventId">Event ID to query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available seats</returns>
    /// <response code="200">Available seats retrieved successfully</response>
    /// <response code="404">Event not found</response>
    /// <response code="500">Server error</response>
    [HttpGet("{eventId:guid}/available-seats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAvailableSeats(
        [FromRoute] Guid eventId,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
            return BadRequest("Event ID cannot be empty");

        var query = new GetAvailableSeatsQuery(eventId);
        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess 
            ? Ok(result)
            : NotFound(result);
    }
}
