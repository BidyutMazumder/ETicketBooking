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
        var price = Money.Create(150.00m, "USD");

        // Act
        var result = Seat.Create(row, number, type, price);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var seat = result.Value;
        seat.Row.Should().Be(row);
        seat.Number.Should().Be(number);
        seat.Type.Should().Be(type);
        seat.Price.Should().Be(price);
        seat.Status.Should().Be(SeatStatus.Available);
    }

    [Fact]
    public void Create_WithEmptyRow_ReturnsFailure()
    {
        // Act
        var result = Seat.Create(string.Empty, 1, SeatType.VIP, Money.Create(150.00m, "USD"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Seat.InvalidRow");
    }

    [Fact]
    public void Create_WithZeroNumber_ReturnsFailure()
    {
        // Act
        var result = Seat.Create("A", 0, SeatType.VIP, Money.Create(150.00m, "USD"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Seat.InvalidNumber");
    }

    [Fact]
    public void Create_WithNullPrice_ReturnsFailure()
    {
        // Act
        var result = Seat.Create("A", 1, SeatType.VIP, null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Seat.NullPrice");
    }

    #endregion

    #region Seat.Hold

    [Fact]
    public void Hold_FromAvailable_TransitionsToHeld()
    {
        // Arrange
        var result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = result.Value;
        var duration = TimeSpan.FromMinutes(10);

        // Act
        var holdResult = seat.Hold(duration);

        // Assert
        holdResult.IsSuccess.Should().BeTrue();
        seat.Status.Should().Be(SeatStatus.Held);
        seat.HeldUntilUtc.Should().BeCloseTo(DateTime.UtcNow.Add(duration), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Hold_FromHeld_ReturnsFailure()
    {
        // Arrange
        var result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = result.Value;
        seat.Hold(TimeSpan.FromMinutes(10));

        // Act
        var holdResult = seat.Hold(TimeSpan.FromMinutes(10));

        // Assert
        holdResult.IsFailure.Should().BeTrue();
        holdResult.Error.Code.Should().Be("Seat.CannotHold");
    }

    [Fact]
    public void Hold_FromReserved_ReturnsFailure()
    {
        // Arrange
        var result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = result.Value;
        seat.Hold(TimeSpan.FromMinutes(10));
        seat.Reserve();

        // Act
        var holdResult = seat.Hold(TimeSpan.FromMinutes(10));

        // Assert
        holdResult.IsFailure.Should().BeTrue();
        holdResult.Error.Code.Should().Be("Seat.CannotHold");
    }

    [Fact]
    public void Hold_FromSold_ReturnsFailure()
    {
        // Arrange
        var result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = result.Value;
        seat.Hold(TimeSpan.FromMinutes(10));
        seat.Reserve();
        seat.Sell();

        // Act
        var holdResult = seat.Hold(TimeSpan.FromMinutes(10));

        // Assert
        holdResult.IsFailure.Should().BeTrue();
        holdResult.Error.Code.Should().Be("Seat.CannotHold");
    }

    #endregion

    #region Seat.Release

    [Fact]
    public void Release_FromHeld_TransitionsToAvailable()
    {
        // Arrange
        var result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = result.Value;
        seat.Hold(TimeSpan.FromMinutes(10));

        // Act
        var releaseResult = seat.Release();

        // Assert
        releaseResult.IsSuccess.Should().BeTrue();
        seat.Status.Should().Be(SeatStatus.Available);
        seat.HeldUntilUtc.Should().BeNull();
    }

    [Fact]
    public void Release_FromAvailable_ReturnsFailure()
    {
        // Arrange
        var result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = result.Value;

        // Act
        var releaseResult = seat.Release();

        // Assert
        releaseResult.IsFailure.Should().BeTrue();
        releaseResult.Error.Code.Should().Be("Seat.CannotRelease");
    }

    #endregion

    #region Seat.Reserve

    [Fact]
    public void Reserve_FromHeld_TransitionsToReserved()
    {
        // Arrange
        var result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = result.Value;
        seat.Hold(TimeSpan.FromMinutes(10));

        // Act
        var reserveResult = seat.Reserve();

        // Assert
        reserveResult.IsSuccess.Should().BeTrue();
        seat.Status.Should().Be(SeatStatus.Reserved);
        seat.HeldUntilUtc.Should().BeNull();
    }

    [Fact]
    public void Reserve_FromAvailable_ReturnsFailure()
    {
        // Arrange
        var result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = result.Value;

        // Act
        var reserveResult = seat.Reserve();

        // Assert
        reserveResult.IsFailure.Should().BeTrue();
        reserveResult.Error.Code.Should().Be("Seat.CannotReserve");
    }

    #endregion

    #region Seat.Sell

    [Fact]
    public void Sell_FromReserved_TransitionsToSold()
    {
        // Arrange
        var result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = result.Value;
        seat.Hold(TimeSpan.FromMinutes(10));
        seat.Reserve();

        // Act
        var sellResult = seat.Sell();

        // Assert
        sellResult.IsSuccess.Should().BeTrue();
        seat.Status.Should().Be(SeatStatus.Sold);
    }

    [Fact]
    public void Sell_FromAvailable_ReturnsFailure()
    {
        // Arrange
        var result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = result.Value;

        // Act
        var sellResult = seat.Sell();

        // Assert
        sellResult.IsFailure.Should().BeTrue();
        sellResult.Error.Code.Should().Be("Seat.CannotSell");
    }

    #endregion

    #region Seat.IsHoldExpired

    [Fact]
    public void IsHoldExpired_WhenHoldHasExpired_ReturnsTrue()
    {
        // Arrange
        var result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = result.Value;
        seat.Hold(TimeSpan.FromMilliseconds(100));

        // Act
        System.Threading.Thread.Sleep(150);
        var isExpired = seat.IsHoldExpired();

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsHoldExpired_WhenHoldHasNotExpired_ReturnsFalse()
    {
        // Arrange
        var result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = result.Value;
        seat.Hold(TimeSpan.FromMinutes(10));

        // Act
        var isExpired = seat.IsHoldExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    #endregion
}
