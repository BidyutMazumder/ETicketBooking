using Booking.Application.Common.Interfaces;
using Booking.Application.Common.Mappings;
using Booking.Application.DTOs;
using Booking.Application.Features.Reservations.Commands.ConfirmBooking;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Domain.ValueObjects;

namespace Booking.Tests.Application.Features.Reservations.Commands;

public sealed class ConfirmBookingCommandHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IReservationRepository> _reservationRepositoryMock;
    private readonly Mock<IReservationMapper> _mapperMock;
    private readonly Mock<ILogger<ConfirmBookingCommandHandler>> _loggerMock;
    private readonly ConfirmBookingCommandHandler _handler;

    public ConfirmBookingCommandHandlerTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _reservationRepositoryMock = new Mock<IReservationRepository>();
        _mapperMock = new Mock<IReservationMapper>();
        _loggerMock = new Mock<ILogger<ConfirmBookingCommandHandler>>();
        _handler = new ConfirmBookingCommandHandler(
            _eventRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidPendingReservation_ReturnsSuccessResponse()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var command = new ConfirmBookingCommand(reservationId);

        var reservation = Reservation.Create(userId, eventId, seatId, TimeSpan.FromMinutes(10));
        // Set payment status to Paid to pass the payment verification
        reservation.MarkAsPaid("pi_test_12345");

        var eventResult = Event.Create("Test Event", "Description", DateTime.UtcNow.AddDays(30), "Venue");
        var @event = eventResult.Value;
        var seatResult = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = seatResult.Value;
        seat.Hold(TimeSpan.FromMinutes(10));
        @event.AddSeat(seat);

        var reservationDto = new ReservationDto(
            reservationId,
            userId,
            eventId,
            seatId,
            "Confirmed",
            DateTime.UtcNow.AddMinutes(10),
            DateTime.UtcNow);

        _reservationRepositoryMock
            .Setup(x => x.GetByIdWithLockAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _eventRepositoryMock
            .Setup(x => x.GetByIdWithLockAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _eventRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _reservationRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mapperMock
            .Setup(x => x.MapToDto(It.IsAny<Reservation>()))
            .Returns(reservationDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(reservationDto);
    }

    [Fact]
    public async Task Handle_WithNonExistentReservation_ReturnsFailureResponse()
    {
        // Arrange
        var command = new ConfirmBookingCommand(Guid.NewGuid());

        _reservationRepositoryMock
            .Setup(x => x.GetByIdWithLockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithExpiredHold_ReturnsFailureResponse()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var command = new ConfirmBookingCommand(reservationId);

        // Create reservation with normal hold duration, then manually set expired time
        var reservation = Reservation.Create(userId, eventId, seatId, TimeSpan.FromMinutes(10));
        reservation.MarkAsPaid("pi_test_12345");
        // Set the hold to have already expired by setting it to the past
        reservation.GetType().GetProperty("HoldExpiresAtUtc")?.SetValue(reservation, DateTime.UtcNow.AddMinutes(-1));

        _reservationRepositoryMock
            .Setup(x => x.GetByIdWithLockAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}
