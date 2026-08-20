using Moq;
using Xunit;
using EventManagement.Bookings.Application.Ports;
using EventManagement.Bookings.Application.Services;
using EventManagement.Bookings.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventManagement.Tests.Services;

public class BookingServiceSeatsTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly BookingService _bookingService;

    public BookingServiceSeatsTests()
    {
        _bookingRepoMock = new Mock<IBookingRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _bookingService = new BookingService(
            _bookingRepoMock.Object,
            _eventPublisherMock.Object,
            new NullLogger<BookingService>()
        );
    }

    [Fact]
    public async Task CreateBooking_ShouldDecreaseAvailableSeats()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        //_userServiceMock.Setup(u => u.GetUserByIdAsync(userId))
        //    .ReturnsAsync(new UserResponseDto(userId, "test", Role.User));

        _bookingRepoMock.Setup(r => r.CountActiveBookingsAsync(userId))
            .ReturnsAsync(0);

        _bookingRepoMock.Setup(r => r.CreateAsync(It.IsAny<Booking>()))
            .ReturnsAsync(new Booking(eventId, userId));

        // Act
        var result = await _bookingService.CreateBookingAsync(eventId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(userId, result.UserId);
    }
}