namespace Booking.Application.Features.Reservations.Commands.HoldSeat;

public sealed class HoldSeatCommandValidator : AbstractValidator<HoldSeatCommand>
{
    public HoldSeatCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("EventId is required");

        RuleFor(x => x.SeatId)
            .NotEmpty().WithMessage("SeatId is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}
