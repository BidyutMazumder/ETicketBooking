namespace Booking.Tests.Domain.Reservations;

public sealed class ReservationTests
{
    #region Reservation.Create

    [Fact]
    public void Create_WithValidInputs_ReturnsReservation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var holdDuration = TimeSpan.FromMinutes(10);

        // Act
        var reservation = Reservation.Create(userId, eventId, seatId, holdDuration);

        // Assert
        reservation.Should().NotBeNull();
        reservation.UserId.Should().Be(userId);
        reservation.EventId.Should().Be(eventId);
        reservation.SeatId.Should().Be(seatId);
        reservation.Status.Should().Be(ReservationStatus.Pending);
        reservation.HoldExpiresAtUtc.Should().BeCloseTo(
            DateTime.UtcNow.Add(holdDuration),
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsException()
    {
        // Act & Assert
        var act = () => Reservation.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromMinutes(10));
        act.Should().Throw<DomainException>()
            .WithMessage("*UserId cannot be empty*");
    }

    [Fact]
    public void Create_WithEmptyEventId_ThrowsException()
    {
        // Act & Assert
        var act = () => Reservation.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), TimeSpan.FromMinutes(10));
        act.Should().Throw<DomainException>()
            .WithMessage("*EventId cannot be empty*");
    }

    [Fact]
    public void Create_WithEmptySeatId_ThrowsException()
    {
        // Act & Assert
        var act = () => Reservation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, TimeSpan.FromMinutes(10));
        act.Should().Throw<DomainException>()
            .WithMessage("*SeatId cannot be empty*");
    }

    [Fact]
    public void Create_WithNegativeHoldDuration_ThrowsException()
    {
        // Act & Assert
        var act = () => Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TimeSpan.FromMinutes(-10));
        act.Should().Throw<DomainException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void Create_WithZeroHoldDuration_ThrowsException()
    {
        // Act & Assert
        var act = () => Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TimeSpan.Zero);
        act.Should().Throw<DomainException>();
    }

    #endregion

    #region Reservation.Confirm

    [Fact]
    public void Confirm_FromPending_TransitionsToConfirmed()
    {
        // Arrange
        var reservation = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TimeSpan.FromMinutes(10));

        // Act
        reservation.Confirm();

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
    }

    [Fact]
    public void Confirm_FromConfirmed_ThrowsException()
    {
        // Arrange
        var reservation = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TimeSpan.FromMinutes(10));
        reservation.Confirm();

        // Act & Assert
        var act = () => reservation.Confirm();
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot confirm*");
    }

    [Fact]
    public void Confirm_FromCancelled_ThrowsException()
    {
        // Arrange
        var reservation = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TimeSpan.FromMinutes(10));
        reservation.Cancel();

        // Act & Assert
        var act = () => reservation.Confirm();
        act.Should().Throw<DomainException>();
    }

    #endregion

    #region Reservation.Cancel

    [Fact]
    public void Cancel_FromPending_TransitionsToCancelled()
    {
        // Arrange
        var reservation = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TimeSpan.FromMinutes(10));

        // Act
        reservation.Cancel();

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromConfirmed_TransitionsToCancelled()
    {
        // Arrange
        var reservation = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TimeSpan.FromMinutes(10));
        reservation.Confirm();

        // Act
        reservation.Cancel();

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsException()
    {
        // Arrange
        var reservation = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TimeSpan.FromMinutes(10));
        reservation.Cancel();

        // Act & Assert
        var act = () => reservation.Cancel();
        act.Should().Throw<DomainException>()
            .WithMessage("*already cancelled*");
    }

    #endregion

    #region Reservation.IsHoldExpired

    [Fact]
    public void IsHoldExpired_WhenPending_WithFutureExpiry_ReturnsFalse()
    {
        // Arrange
        var reservation = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TimeSpan.FromMinutes(10));

        // Act
        var result = reservation.IsHoldExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsHoldExpired_WhenPending_WithPastExpiry_ReturnsTrue()
    {
        // Arrange
        var reservation = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TimeSpan.FromMilliseconds(-100)); // Already expired

        // Act
        var result = reservation.IsHoldExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsHoldExpired_WhenConfirmed_ReturnsFalse()
    {
        // Arrange
        var reservation = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TimeSpan.FromMinutes(10));
        reservation.Confirm();

        // Act
        var result = reservation.IsHoldExpired();

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
