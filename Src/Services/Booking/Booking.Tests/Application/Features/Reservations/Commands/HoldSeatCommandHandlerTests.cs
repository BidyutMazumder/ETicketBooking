using Booking.Application.Common.Interfaces;
using Booking.Application.Common.Mappings;
using Booking.Application.DTOs;
using Booking.Application.Features.Reservations.Commands.HoldSeat;
using Microsoft.Extensions.Logging;

namespace Booking.Tests.Application.Features.Reservations.Commands;

public sealed class HoldSeatCommandHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IReservationRepository> _reservationRepositoryMock;
    private readonly Mock<IReservationMapper> _mapperMock;
    private readonly Mock<ILogger<HoldSeatCommandHandler>> _loggerMock;
    private readonly HoldSeatCommandHandler _handler;

    public HoldSeatCommandHandlerTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _reservationRepositoryMock = new Mock<IReservationRepository>();
        _mapperMock = new Mock<IReservationMapper>();
        _loggerMock = new Mock<ILogger<HoldSeatCommandHandler>>();
        _handler = new HoldSeatCommandHandler(
            _eventRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var command = new HoldSeatCommand(eventId, seatId, userId);

        var @event = Event.Create("Test Event", "Description", DateTime.UtcNow.AddDays(30), "Venue");
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        @event.AddSeat(seat);

        var reservation = Reservation.Create(userId, eventId, seatId, TimeSpan.FromMinutes(10));
        var reservationDto = new ReservationDto(
            reservation.Id,
            userId,
            eventId,
            seatId,
            "Pending",
            DateTime.UtcNow.AddMinutes(10),
            DateTime.UtcNow);

        _eventRepositoryMock
            .Setup(x => x.GetByIdWithLockAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _reservationRepositoryMock
            .Setup(x => x.GetByEventAndSeatWithLockAsync(eventId, seatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        _eventRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _reservationRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
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
    public async Task Handle_WithNonExistentEvent_ReturnsFailureResponse()
    {
        // Arrange
        var command = new HoldSeatCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _eventRepositoryMock
            .Setup(x => x.GetByIdWithLockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithUnavailableSeat_ReturnsFailureResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var command = new HoldSeatCommand(eventId, seatId, userId);

        var @event = Event.Create("Test Event", "Description", DateTime.UtcNow.AddDays(30), "Venue");
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        seat.Hold(TimeSpan.FromMinutes(10)); // Already held
        @event.AddSeat(seat);

        _eventRepositoryMock
            .Setup(x => x.GetByIdWithLockAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}
