namespace Identity.API.Controllers;

using Identity.Application.Common.Exceptions;
using Identity.Application.Features.Users.Commands.CreateUser;
using Identity.Application.Features.Users.Commands.DeleteUser;
using Identity.Application.Features.Users.Commands.UpdateUser;
using Identity.Application.Features.Users.Queries;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Shared.Kernel.Domain.Abstractions;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<Response<UserDto>>> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _sender.Send(command, cancellationToken);

            return result.IsSuccess && result.Data is not null
                ? CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result)
                : BadRequest(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Response<UserDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("email/{email}")]
    public async Task<ActionResult<Response<UserDto>>> GetByEmail(
        string email,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserByEmailQuery(email), cancellationToken);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedRes<UserDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAllUsersQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Response<UserDto>>> Update(
        Guid id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var updateCommand = new UpdateUserCommand(id, command.FirstName, command.LastName, command.Role);
            var result = await _sender.Send(updateCommand, cancellationToken);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Response<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteUserCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(result);
    }
}
