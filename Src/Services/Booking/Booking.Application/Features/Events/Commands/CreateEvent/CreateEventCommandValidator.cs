namespace Booking.Application.Features.Events.Commands.CreateEvent;

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.StartDateTime)
            .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future");

        RuleFor(x => x.VenueName)
            .NotEmpty().WithMessage("Venue name is required")
            .MaximumLength(200).WithMessage("Venue name must not exceed 200 characters");

        RuleForEach(x => x.CategoryWiseSeats)
            .ChildRules(seat =>
            {
                seat.RuleFor(s => s.Category)
                    .NotEmpty().WithMessage("Category is required");

                seat.RuleFor(s => s.SeatType)
                    .NotEmpty().WithMessage("Seat type is required")
                    .Must(st => st == "VIP" || st == "Regular" || st == "Economy")
                    .WithMessage("Seat type must be VIP, Regular, or Economy");

                seat.RuleFor(s => s.Count)
                    .GreaterThan(0).WithMessage("Seat count must be greater than 0")
                    .LessThanOrEqualTo(1000).WithMessage("Seat count must not exceed 1000");

                seat.RuleFor(s => s.Row)
                    .NotEmpty().WithMessage("Row is required")
                    .MaximumLength(10).WithMessage("Row must not exceed 10 characters");

                seat.RuleFor(s => s.Price)
                    .GreaterThan(0).WithMessage("Price must be greater than 0");
            })
            .When(x => x.CategoryWiseSeats != null);
    }
}


