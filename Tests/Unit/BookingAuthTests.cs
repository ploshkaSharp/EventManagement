using Xunit;
using Moq;
using EventManagement.Bookings.Application.Ports;
using EventManagement.Bookings.Application.Services;
using EventManagement.Bookings.Domain.Entities;
using EventManagement.Bookings.Domain.Enums;
using EventManagement.Bookings.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventManagement.Tests.Services;

public class BookingAuthorizationTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock;    
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly BookingService _bookingService;

    public BookingAuthorizationTests()
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
    public async Task CancelBooking_OwnerCancelsOwnBooking_ShouldSucceed()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var booking = new Booking(eventId, userId);
        typeof(Booking).GetProperty("Id")?.SetValue(booking, bookingId);

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId))
            .ReturnsAsync(booking);

        _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _bookingService.CancelBookingAsync(bookingId, userId, false);

        // Assert
        Assert.True(result);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public async Task CancelBooking_UserCancelsOtherUserBooking_ShouldThrowUnAuthorizedOperationException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var booking = new Booking(eventId, ownerId);
        typeof(Booking).GetProperty("Id")?.SetValue(booking, bookingId);

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId))
            .ReturnsAsync(booking);

        // Act & Assert
        await Assert.ThrowsAsync<UnAuthorizedOperationException>(() =>
            _bookingService.CancelBookingAsync(bookingId, otherUserId, false));
    }

    [Fact]
    public async Task CancelBooking_AdminCancelsOtherUserBooking_ShouldSucceed()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var booking = new Booking(eventId, userId);
        typeof(Booking).GetProperty("Id")?.SetValue(booking, bookingId);

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId))
            .ReturnsAsync(booking);

        _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _bookingService.CancelBookingAsync(bookingId, adminId, true);

        // Assert
        Assert.True(result);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }
}