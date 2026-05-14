namespace Booking.Tests.Domain.Events;

public sealed class EventTests
{
    #region Event.Create

    [Fact]
    public void Create_WithValidInputs_ReturnsEvent()
    {
        // Arrange
        var title = "Taylor Swift - Eras Tour";
        var description = "The biggest concert of the year";
        var startDateTime = DateTime.UtcNow.AddDays(30);
        var venueName = "MetLife Stadium";

        // Act
        var @event = Event.Create(title, description, startDateTime, venueName);

        // Assert
        @event.Should().NotBeNull();
        @event.Title.Should().Be(title);
        @event.Description.Should().Be(description);
        @event.StartDateTime.Should().Be(startDateTime);
        @event.VenueName.Should().Be(venueName);
        @event.IsPublished.Should().BeFalse();
        @event.Seats.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyTitle_ThrowsException()
    {
        // Arrange
        var title = string.Empty;
        var description = "Valid description";
        var startDateTime = DateTime.UtcNow.AddDays(30);
        var venueName = "Valid Venue";

        // Act & Assert
        var act = () => Event.Create(title, description, startDateTime, venueName);
        act.Should().Throw<DomainException>()
            .WithMessage("Event title cannot be empty");
    }

    [Fact]
    public void Create_WithNullTitle_ThrowsException()
    {
        // Arrange
        string? title = null;
        var description = "Valid description";
        var startDateTime = DateTime.UtcNow.AddDays(30);
        var venueName = "Valid Venue";

        // Act & Assert
        var act = () => Event.Create(title!, description, startDateTime, venueName);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithPastDateTime_ThrowsException()
    {
        // Arrange
        var title = "Past Event";
        var description = "Valid description";
        var startDateTime = DateTime.UtcNow.AddDays(-1);
        var venueName = "Valid Venue";

        // Act & Assert
        var act = () => Event.Create(title, description, startDateTime, venueName);
        act.Should().Throw<DomainException>()
            .WithMessage("Event start date must be in the future");
    }

    [Fact]
    public void Create_WithCurrentDateTime_ThrowsException()
    {
        // Arrange
        var title = "Current Event";
        var description = "Valid description";
        var startDateTime = DateTime.UtcNow;
        var venueName = "Valid Venue";

        // Act & Assert
        var act = () => Event.Create(title, description, startDateTime, venueName);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithEmptyDescription_ThrowsException()
    {
        // Arrange
        var title = "Valid Title";
        var description = string.Empty;
        var startDateTime = DateTime.UtcNow.AddDays(30);
        var venueName = "Valid Venue";

        // Act & Assert
        var act = () => Event.Create(title, description, startDateTime, venueName);
        act.Should().Throw<DomainException>()
            .WithMessage("Event description cannot be empty");
    }

    [Fact]
    public void Create_WithEmptyVenueName_ThrowsException()
    {
        // Arrange
        var title = "Valid Title";
        var description = "Valid description";
        var startDateTime = DateTime.UtcNow.AddDays(30);
        var venueName = string.Empty;

        // Act & Assert
        var act = () => Event.Create(title, description, startDateTime, venueName);
        act.Should().Throw<DomainException>()
            .WithMessage("Event venue name cannot be empty");
    }

    #endregion

    #region Event.AddSeat

    [Fact]
    public void AddSeat_WithValidSeat_AddsSeatToEvent()
    {
        // Arrange
        var @event = CreateValidEvent();
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);

        // Act
        @event.AddSeat(seat);

        // Assert
        @event.Seats.Should().HaveCount(1);
        @event.Seats.First().Should().Be(seat);
    }

    [Fact]
    public void AddSeat_WithDuplicateSeat_ThrowsException()
    {
        // Arrange
        var @event = CreateValidEvent();
        var seat1 = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        var seat2 = Seat.Create("A", 1, SeatType.VIP, 150.00m);

        @event.AddSeat(seat1);

        // Act & Assert
        var act = () => @event.AddSeat(seat2);
        act.Should().Throw<DomainException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void AddSeat_WithNullSeat_ThrowsException()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act & Assert
        var act = () => @event.AddSeat(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Event.RemoveSeat

    [Fact]
    public void RemoveSeat_WithExistingSeat_RemovesSeat()
    {
        // Arrange
        var @event = CreateValidEvent();
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        @event.AddSeat(seat);

        // Act
        @event.RemoveSeat(seat);

        // Assert
        @event.Seats.Should().BeEmpty();
    }

    #endregion

    #region Event.GetSeat

    [Fact]
    public void GetSeat_ByIdWithExistingSeat_ReturnsSeat()
    {
        // Arrange
        var @event = CreateValidEvent();
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        @event.AddSeat(seat);

        // Act
        var result = @event.GetSeat(seat.Id);

        // Assert
        result.Should().Be(seat);
    }

    [Fact]
    public void GetSeat_ByIdWithNonExistentId_ReturnsNull()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act
        var result = @event.GetSeat(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetSeat_ByRowAndNumberWithExistingSeat_ReturnsSeat()
    {
        // Arrange
        var @event = CreateValidEvent();
        var seat = Seat.Create("A", 1, SeatType.VIP, 150.00m);
        @event.AddSeat(seat);

        // Act
        var result = @event.GetSeat("A", 1);

        // Assert
        result.Should().Be(seat);
    }

    #endregion

    #region Event.Publish

    [Fact]
    public void Publish_WithSeats_PublishesEvent()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.AddSeat(Seat.Create("A", 1, SeatType.VIP, 150.00m));

        // Act
        @event.Publish();

        // Assert
        @event.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Publish_WithoutSeats_ThrowsException()
    {
        // Arrange
        var @event = CreateValidEvent();

        // Act & Assert
        var act = () => @event.Publish();
        act.Should().Throw<DomainException>()
            .WithMessage("*without seats*");
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_ThrowsException()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.AddSeat(Seat.Create("A", 1, SeatType.VIP, 150.00m));
        @event.Publish();

        // Act & Assert
        var act = () => @event.Publish();
        act.Should().Throw<DomainException>()
            .WithMessage("*already published*");
    }

    #endregion

    #region Event.UpdateDetails

    [Fact]
    public void UpdateDetails_WhenNotPublished_UpdatesDetails()
    {
        // Arrange
        var @event = CreateValidEvent();
        var newTitle = "Updated Title";
        var newDescription = "Updated Description";
        var newVenue = "Updated Venue";

        // Act
        @event.UpdateDetails(newTitle, newDescription, newVenue);

        // Assert
        @event.Title.Should().Be(newTitle);
        @event.Description.Should().Be(newDescription);
        @event.VenueName.Should().Be(newVenue);
    }

    [Fact]
    public void UpdateDetails_WhenPublished_ThrowsException()
    {
        // Arrange
        var @event = CreateValidEvent();
        @event.AddSeat(Seat.Create("A", 1, SeatType.VIP, 150.00m));
        @event.Publish();

        // Act & Assert
        var act = () => @event.UpdateDetails("New Title", "New Desc", "New Venue");
        act.Should().Throw<DomainException>()
            .WithMessage("*published*");
    }

    #endregion

    #region Helpers

    private static Event CreateValidEvent()
    {
        return Event.Create(
            "Test Event",
            "Test Description",
            DateTime.UtcNow.AddDays(30),
            "Test Venue");
    }

    #endregion
}
