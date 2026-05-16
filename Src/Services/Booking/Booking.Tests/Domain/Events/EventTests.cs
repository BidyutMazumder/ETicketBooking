namespace Booking.Tests.Domain.Events;

public sealed class EventTests
{
    #region Event.Create

    [Fact]
    public void Create_WithValidInputs_ReturnsEvent()
    {
        // Arrange
        var title = "Concert 2024";
        var description = "Amazing concert";
        var startDateTime = DateTime.UtcNow.AddDays(30);
        var venueName = "Madison Square Garden";

        // Act
        var result = Event.Create(title, description, startDateTime, venueName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var @event = result.Value;
        @event.Title.Should().Be(title);
        @event.Description.Should().Be(description);
        @event.StartDateTime.Should().Be(startDateTime);
        @event.VenueName.Should().Be(venueName);
        @event.IsPublished.Should().BeFalse();
        @event.Seats.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyTitle_ReturnsFailure()
    {
        // Act
        var result = Event.Create(string.Empty, "Description", DateTime.UtcNow.AddDays(1), "Venue");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Event.InvalidTitle");
    }

    [Fact]
    public void Create_WithPastDateTime_ReturnsFailure()
    {
        // Act
        var result = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(-1), "Venue");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Event.InvalidStartDate");
    }

    [Fact]
    public void Create_WithEmptyVenue_ReturnsFailure()
    {
        // Act
        var result = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Event.InvalidVenue");
    }

    #endregion

    #region Event.AddSeat

    [Fact]
    public void AddSeat_WithValidSeat_AddsToCollection()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;
        var seatResult = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = seatResult.Value;

        // Act
        var addResult = @event.AddSeat(seat);

        // Assert
        addResult.IsSuccess.Should().BeTrue();
        @event.Seats.Should().HaveCount(1);
        @event.Seats.First().Should().Be(seat);
    }

    [Fact]
    public void AddSeat_WithDuplicateSeat_ReturnsFailure()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;
        var seatResult1 = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seatResult2 = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));

        @event.AddSeat(seatResult1.Value);

        // Act
        var addResult = @event.AddSeat(seatResult2.Value);

        // Assert
        addResult.IsFailure.Should().BeTrue();
        addResult.Error.Code.Should().Be("Event.DuplicateSeat");
    }

    #endregion

    #region Event.RemoveSeat

    [Fact]
    public void RemoveSeat_WithExistingSeat_RemovesFromCollection()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;
        var seatResult = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = seatResult.Value;
        @event.AddSeat(seat);

        // Act
        var removeResult = @event.RemoveSeat(seat);

        // Assert
        removeResult.IsSuccess.Should().BeTrue();
        @event.Seats.Should().BeEmpty();
    }

    [Fact]
    public void RemoveSeat_WithNonExistingSeat_ReturnsFailure()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;
        var seatResult = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = seatResult.Value;

        // Act
        var removeResult = @event.RemoveSeat(seat);

        // Assert
        removeResult.IsFailure.Should().BeTrue();
        removeResult.Error.Code.Should().Be("Event.SeatNotFound");
    }

    #endregion

    #region Event.GetSeat

    [Fact]
    public void GetSeat_WithValidId_ReturnsSeat()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;
        var seatResult = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = seatResult.Value;
        @event.AddSeat(seat);

        // Act
        var foundSeat = @event.GetSeat(seat.Id);

        // Assert
        foundSeat.Should().NotBeNull();
        foundSeat.Should().Be(seat);
    }

    [Fact]
    public void GetSeatByRowAndNumber_WithValidRowAndNumber_ReturnsSeat()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;
        var seatResult = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat = seatResult.Value;
        @event.AddSeat(seat);

        // Act
        var foundSeat = @event.GetSeatByRowAndNumber("A", 1);

        // Assert
        foundSeat.Should().NotBeNull();
        foundSeat.Should().Be(seat);
    }

    #endregion

    #region Event.PublishEvent

    [Fact]
    public void PublishEvent_WithSeats_ChangesIsPublishedToTrue()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;
        var seatResult = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        @event.AddSeat(seatResult.Value);

        // Act
        var publishResult = @event.PublishEvent();

        // Assert
        publishResult.IsSuccess.Should().BeTrue();
        @event.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void PublishEvent_WithoutSeats_ReturnsFailure()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;

        // Act
        var publishResult = @event.PublishEvent();

        // Assert
        publishResult.IsFailure.Should().BeTrue();
        publishResult.Error.Code.Should().Be("Event.NoSeats");
    }

    [Fact]
    public void PublishEvent_WhenAlreadyPublished_ReturnsFailure()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;
        var seatResult = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        @event.AddSeat(seatResult.Value);
        @event.PublishEvent();

        // Act
        var publishResult = @event.PublishEvent();

        // Assert
        publishResult.IsFailure.Should().BeTrue();
        publishResult.Error.Code.Should().Be("Event.AlreadyPublished");
    }

    #endregion

    #region Event.UnpublishEvent

    [Fact]
    public void UnpublishEvent_WhenPublished_ChangesIsPublishedToFalse()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;
        var seatResult = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        @event.AddSeat(seatResult.Value);
        @event.PublishEvent();

        // Act
        var unpublishResult = @event.UnpublishEvent();

        // Assert
        unpublishResult.IsSuccess.Should().BeTrue();
        @event.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void UnpublishEvent_WhenNotPublished_ReturnsFailure()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;

        // Act
        var unpublishResult = @event.UnpublishEvent();

        // Assert
        unpublishResult.IsFailure.Should().BeTrue();
        unpublishResult.Error.Code.Should().Be("Event.NotPublished");
    }

    #endregion

    #region Event.UpdateDetails

    [Fact]
    public void UpdateDetails_WithValidDetails_UpdatesProperties()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;
        var newTitle = "New Title";
        var newDescription = "New Description";
        var newVenue = "New Venue";

        // Act
        var updateResult = @event.UpdateDetails(newTitle, newDescription, newVenue);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        @event.Title.Should().Be(newTitle);
        @event.Description.Should().Be(newDescription);
        @event.VenueName.Should().Be(newVenue);
        @event.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Event.GenerateSeats

    [Fact]
    public void GenerateSeats_WithValidPlan_GeneratesAllSeats()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;

        var rowA = SeatingPlanRow.Create("A", 1, 5, SeatType.VIP, Money.Create(150.00m, "USD")).Value;
        var rowB = SeatingPlanRow.Create("B", 1, 10, SeatType.Regular, Money.Create(75.00m, "USD")).Value;
        var planResult = SeatingPlan.Create(rowA, rowB);
        var plan = planResult.Value;

        // Act
        var generateResult = @event.GenerateSeats(plan);

        // Assert
        generateResult.IsSuccess.Should().BeTrue();
        @event.TotalSeats.Should().Be(15);
    }

    [Fact]
    public void GenerateSeats_WhenSeatsAlreadyGenerated_ReturnsFailure()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;
        var seatResult = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        @event.AddSeat(seatResult.Value);

        var rowA = SeatingPlanRow.Create("C", 1, 5, SeatType.VIP, Money.Create(150.00m, "USD")).Value;
        var planResult = SeatingPlan.Create(rowA);
        var plan = planResult.Value;

        // Act
        var generateResult = @event.GenerateSeats(plan);

        // Assert
        generateResult.IsFailure.Should().BeTrue();
        generateResult.Error.Code.Should().Be("Event.SeatsAlreadyGenerated");
    }

    #endregion

    #region Event.GetTotalRevenue

    [Fact]
    public void GetTotalRevenue_WithSoldSeats_CalculatesCorrectly()
    {
        // Arrange
        var eventResult = Event.Create("Title", "Description", DateTime.UtcNow.AddDays(1), "Venue");
        var @event = eventResult.Value;

        var seat1Result = Seat.Create("A", 1, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat1 = seat1Result.Value;
        seat1.Hold(TimeSpan.FromMinutes(10));
        seat1.Reserve();
        seat1.Sell();

        var seat2Result = Seat.Create("A", 2, SeatType.VIP, Money.Create(150.00m, "USD"));
        var seat2 = seat2Result.Value;
        seat2.Hold(TimeSpan.FromMinutes(10));
        seat2.Reserve();
        seat2.Sell();

        @event.AddSeat(seat1);
        @event.AddSeat(seat2);

        // Act
        var revenue = @event.GetTotalRevenue();

        // Assert
        revenue.Amount.Should().Be(300.00m);
        revenue.Currency.Should().Be("USD");
    }

    #endregion
}
