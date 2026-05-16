namespace Booking.API.Controllers;

using Booking.Application.Features.SeatCategories.Commands.ApplyDiscountCommand;
using Booking.Application.Features.SeatCategories.Commands.CreateSeatCategory;
using Booking.Application.Features.SeatCategories.Commands.UpdateBasePriceCommand;
using Booking.Application.Features.SeatCategories.Queries.GetAllSeatCategories;
using Booking.Application.Features.SeatCategories.Queries.GetSeatCategoryById;

/// <summary>
/// Seat Category Management Endpoints
/// Handles creation, retrieval, and management of seat categories with pricing
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class SeatCategoriesController : BaseController
{
    public SeatCategoriesController(ISender sender) : base(sender) { }

    /// <summary>
    /// Create a new seat category
    /// </summary>
    /// <remarks>
    /// Creates a new seat category with specified pricing and type.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/seatcategories/create
    ///     {
    ///        "name": "VIP Premium",
    ///        "seatType": "VIP",
    ///        "price": 150.00,
    ///        "currency": "USD",
    ///        "description": "Premium seats with best view"
    ///     }
    /// </remarks>
    /// <param name="command">Seat category creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created seat category with assigned ID</returns>
    /// <response code="201">Seat category created successfully</response>
    /// <response code="400">Invalid input or validation error</response>
    /// <response code="409">Seat category with same name already exists</response>
    /// <response code="500">Server error</response>
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSeatCategoryCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result)
            : result.Error.Code == "SeatCategory.AlreadyExists"
                ? Conflict(result)
                : BadRequest(result);
    }

    /// <summary>
    /// Get all seat categories
    /// </summary>
    /// <remarks>
    /// Retrieves all seat categories in the system, including active and inactive ones.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all seat categories</returns>
    /// <response code="200">Seat categories retrieved successfully</response>
    /// <response code="500">Server error</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetAllSeatCategoriesQuery();
        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess 
            ? Ok(result)
            : StatusCode(500, result);
    }

    /// <summary>
    /// Get a specific seat category by ID
    /// </summary>
    /// <remarks>
    /// Retrieves a single seat category with all its details including pricing and discounts.
    /// </remarks>
    /// <param name="id">Seat category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Seat category details</returns>
    /// <response code="200">Seat category retrieved successfully</response>
    /// <response code="400">Invalid category ID</response>
    /// <response code="404">Seat category not found</response>
    /// <response code="500">Server error</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Seat category ID cannot be empty");

        var query = new GetSeatCategoryByIdQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess 
            ? Ok(result)
            : result.Error.Code == "SeatCategory.NotFound"
                ? NotFound(result)
                : StatusCode(500, result);
    }

    /// <summary>
    /// Update the base price of a seat category
    /// </summary>
    /// <remarks>
    /// Updates the base price for a seat category. The currency must match the current category currency.
    /// 
    /// Sample request:
    /// 
    ///     PUT /api/seatcategories/{id}/price
    ///     {
    ///        "newPrice": 175.00,
    ///        "currency": "USD"
    ///     }
    /// </remarks>
    /// <param name="id">Seat category ID</param>
    /// <param name="command">Price update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated seat category</returns>
    /// <response code="200">Price updated successfully</response>
    /// <response code="400">Invalid input or currency mismatch</response>
    /// <response code="404">Seat category not found</response>
    /// <response code="500">Server error</response>
    [HttpPut("{id:guid}/price")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePrice(
        [FromRoute] Guid id,
        [FromBody] UpdateBasePriceCommand command,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Seat category ID cannot be empty");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updateCommand = new UpdateBasePriceCommand(id, command.NewPrice, command.Currency);
        var result = await _sender.Send(updateCommand, cancellationToken);

        return result.IsSuccess 
            ? Ok(result)
            : result.Error.Code == "SeatCategory.NotFound"
                ? NotFound(result)
                : BadRequest(result);
    }

    /// <summary>
    /// Apply a discount to a seat category
    /// </summary>
    /// <remarks>
    /// Applies a percentage discount to the seat category. The discount is applied to the base price.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/seatcategories/{id}/discount
    ///     {
    ///        "discountPercentage": 10.00
    ///     }
    /// </remarks>
    /// <param name="id">Seat category ID</param>
    /// <param name="command">Discount details (percentage value)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated seat category with new effective price</returns>
    /// <response code="200">Discount applied successfully</response>
    /// <response code="400">Invalid discount percentage (must be 0-100)</response>
    /// <response code="404">Seat category not found</response>
    /// <response code="500">Server error</response>
    [HttpPost("{id:guid}/discount")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApplyDiscount(
        [FromRoute] Guid id,
        [FromBody] ApplyDiscountCommand command,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Seat category ID cannot be empty");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var discountCommand = new ApplyDiscountCommand(id, command.DiscountPercentage);
        var result = await _sender.Send(discountCommand, cancellationToken);

        return result.IsSuccess 
            ? Ok(result)
            : result.Error.Code == "SeatCategory.NotFound"
                ? NotFound(result)
                : BadRequest(result);
    }
}
