namespace Booking.Tests.Domain.Events;

public sealed class SeatTests
{
    #region Seat.Create

    [Fact]
    public void Create_WithValidInputs_ReturnsSeat()
    {
        // Arrange
        var row = "A";
        var number = 1;
        var type = SeatType.VIP;
        var price = 150.00m;

        // Act
        var seat = Seat.Create(row, number, type, price);

        // Assert
        seat.Should().NotBeNull();
        seat.Row.Should().Be(row);
        seat.Number.Should().Be(number);
        seat.Type.Should().Be(type);
        seat.Price.Should().Be(price);
        seat.Status.Should().Be(SeatStatus.Available);
    }

    [Fact]
    public void Create_WithEmptyRow_ThrowsException()
    {
        // Act & Assert
        var act = () => Seat.Create(string.Empty, 1, SeatType.VIP, 150.00m);
        act.Should().Throw<DomainException>()
            .WithMessage("*row cannot be empty*");
    }

    [Fact]
    public void Create_WithZeroNumber_ThrowsException()
    {
        // Act & Assert
        var act = () => Seat.Create("A", 0, SeatType.VIP, 150.00m);
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void Create_WithNegativePrice_ThrowsException()
    {
        // Act & Assert
        var act = () => Seat.Create("A", 1, SeatType.VIP, -10.00m);
        act.Should().Throw<DomainException>()
            .WithMessage("*negative*");
    }

    #endregion

    #region Seat.Hold

    [Fact]
    public void Hold_FromAvailable_TransitionsToHeld()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        var duration = TimeSpan.FromMinutes(10);

        // Act
        seat.Hold(duration);

        // Assert
        seat.Status.Should().Be(SeatStatus.Held);
        seat.HeldUntilUtc.Should().BeCloseTo(DateTime.UtcNow.Add(duration), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Hold_FromHeld_ThrowsException()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        seat.Hold(TimeSpan.FromMinutes(10));

        // Act & Assert
        var act = () => seat.Hold(TimeSpan.FromMinutes(10));
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot hold*");
    }

    [Fact]
    public void Hold_FromReserved_ThrowsException()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        seat.Hold(TimeSpan.FromMinutes(10));
        seat.Reserve();

        // Act & Assert
        var act = () => seat.Hold(TimeSpan.FromMinutes(10));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Hold_FromSold_ThrowsException()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        seat.Hold(TimeSpan.FromMinutes(10));
        seat.Reserve();
        seat.Sell();

        // Act & Assert
        var act = () => seat.Hold(TimeSpan.FromMinutes(10));
        act.Should().Throw<DomainException>();
    }

    #endregion

    #region Seat.Release

    [Fact]
    public void Release_FromHeld_TransitionsToAvailable()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        seat.Hold(TimeSpan.FromMinutes(10));

        // Act
        seat.Release();

        // Assert
        seat.Status.Should().Be(SeatStatus.Available);
        seat.HeldUntilUtc.Should().BeNull();
    }

    [Fact]
    public void Release_FromAvailable_ThrowsException()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);

        // Act & Assert
        var act = () => seat.Release();
        act.Should().Throw<DomainException>()
            .WithMessage("*Only held seats*");
    }

    #endregion

    #region Seat.Reserve

    [Fact]
    public void Reserve_FromHeld_TransitionsToReserved()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        seat.Hold(TimeSpan.FromMinutes(10));

        // Act
        seat.Reserve();

        // Assert
        seat.Status.Should().Be(SeatStatus.Reserved);
    }

    [Fact]
    public void Reserve_FromAvailable_ThrowsException()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);

        // Act & Assert
        var act = () => seat.Reserve();
        act.Should().Throw<DomainException>()
            .WithMessage("*Only held seats*");
    }

    #endregion

    #region Seat.Sell

    [Fact]
    public void Sell_FromReserved_TransitionsToSold()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        seat.Hold(TimeSpan.FromMinutes(10));
        seat.Reserve();

        // Act
        seat.Sell();

        // Assert
        seat.Status.Should().Be(SeatStatus.Sold);
    }

    [Fact]
    public void Sell_FromAvailable_ThrowsException()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);

        // Act & Assert
        var act = () => seat.Sell();
        act.Should().Throw<DomainException>()
            .WithMessage("*Only reserved seats*");
    }

    #endregion

    #region Seat.IsHoldExpired

    [Fact]
    public void IsHoldExpired_WithFutureExpiry_ReturnsFalse()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        seat.Hold(TimeSpan.FromMinutes(10));

        // Act
        var result = seat.IsHoldExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsHoldExpired_WithPastExpiry_ReturnsTrue()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        seat.Hold(TimeSpan.FromMilliseconds(-100)); // Already expired

        // Act
        var result = seat.IsHoldExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsHoldExpired_WhenNotHeld_ReturnsFalse()
    {
        // Arrange
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);

        // Act
        var result = seat.IsHoldExpired();

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
