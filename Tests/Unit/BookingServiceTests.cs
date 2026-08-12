using Moq;
using Xunit;
using EventManagement.Bookings.Application.DTOs;
using EventManagement.Bookings.Application.Ports;
using EventManagement.Bookings.Application.Services;
using EventManagement.Bookings.Domain.Entities;
using EventManagement.Bookings.Domain.Enums;
using EventManagement.Bookings.Domain.Exceptions;
using EventManagement.Shared.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventManagement.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _bookingRepoMock = new Mock<IBookingRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        var logger = new NullLogger<BookingService>();

        _bookingService = new BookingService(
            _bookingRepoMock.Object,
            _eventPublisherMock.Object,
            logger
        );
    }

    [Fact]
    public async Task CreateBookingAsync_WhenUserHasReachedBookingLimit_ShouldThrowBookingLimitExceededException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var maxBookings = 10;

        _bookingRepoMock.Setup(r => r.CountActiveBookingsAsync(userId))
            .ReturnsAsync(maxBookings);

        // Act & Assert
        await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            _bookingService.CreateBookingAsync(eventId, userId));
    }

    [Fact]
    public async Task CreateBookingAsync_WhenUserHasNoActiveBookings_ShouldSucceed()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

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
        Assert.Equal(BookingStatus.Pending, result.Status);

        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BookingRequestedEvent>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBookingResultAsync_Success_ShouldConfirmBooking()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var booking = new Booking(eventId, userId);
        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(booking);
        _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync(booking);

        var processedEvent = new BookingProcessedEvent(
            bookingId,
            eventId,
            userId,
            true,
            null,
            DateTime.UtcNow,
            9
        );

        // Act
        await _bookingService.ProcessBookingResultAsync(processedEvent);

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public async Task ProcessBookingResultAsync_Failure_ShouldRejectBooking()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var booking = new Booking(eventId, userId);
        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(booking);
        _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync(booking);

        var processedEvent = new BookingProcessedEvent(
            bookingId,
            eventId,
            userId,
            false,
            "Not enough seats",
            DateTime.UtcNow,
            0
        );

        // Act
        await _bookingService.ProcessBookingResultAsync(processedEvent);

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }
}