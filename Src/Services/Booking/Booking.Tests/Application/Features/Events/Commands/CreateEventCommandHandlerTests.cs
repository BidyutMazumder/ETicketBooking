using Booking.Application.Common.Interfaces;
using Booking.Application.Common.Mappings;
using Booking.Application.DTOs;
using Booking.Application.Features.Events.Commands.CreateEvent;

namespace Booking.Tests.Application.Features.Events.Commands;

public sealed class CreateEventCommandHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IEventMapper> _mapperMock;
    private readonly CreateEventCommandHandler _handler;

    public CreateEventCommandHandlerTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _mapperMock = new Mock<IEventMapper>();
        _handler = new CreateEventCommandHandler(_eventRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessResponse()
    {
        // Arrange
        var command = new CreateEventCommand(
            "Test Event",
            "Test Description",
            DateTime.UtcNow.AddDays(30),
            "Test Venue");

        var eventDto = new EventDto(
            Guid.NewGuid(),
            command.Title,
            command.Description,
            command.StartDateTime,
            command.VenueName,
            false,
            DateTime.UtcNow);

        _eventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mapperMock
            .Setup(x => x.MapToDto(It.IsAny<Event>()))
            .Returns(eventDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(eventDto);
        _eventRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidCommand_ReturnsFailureResponse()
    {
        // Arrange
        var command = new CreateEventCommand(
            "Test Event",
            "Test Description",
            DateTime.UtcNow.AddDays(-1), // Past date
            "Test Venue");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        //result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithRepositoryException_ReturnsFailureResponse()
    {
        // Arrange
        var command = new CreateEventCommand(
            "Test Event",
            "Test Description",
            DateTime.UtcNow.AddDays(30),
            "Test Venue");

        _eventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await _handler.Handle(command, CancellationToken.None));
    }
}
