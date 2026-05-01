namespace Identity.Application.CommandHandlers;

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserCommand, Response<bool>>
{
    private readonly IUserRepository _userRepository;

    public DeleteUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Response<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
            return Response<bool>.Failure(new Error("Error.UserNotFound", "User not found"));

        user.Delete();
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Response<bool>.Success(true);
    }
}