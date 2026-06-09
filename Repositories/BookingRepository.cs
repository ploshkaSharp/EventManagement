using Microsoft.EntityFrameworkCore;
using EventManagement.Data;
using EventManagement.Models;

namespace EventManagement.Repositories;

public class BookingRepository : IBookingRepository
{
  private readonly AppDbContext _context;
  private readonly ILogger<BookingRepository> _logger;

  public BookingRepository(AppDbContext context, ILogger<BookingRepository> logger)
  {
    _context = context;
    _logger = logger;
  }

  public async Task<Booking?> GetByIdAsync(Guid id)
  {
    return await _context.Bookings
        .FirstOrDefaultAsync(b => b.Id == id);
  }

  public async Task<IEnumerable<Booking>> GetAllAsync()
  {
    return await _context.Bookings
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();
  }

  public async Task<IEnumerable<Booking>> GetByEventIdAsync(Guid eventId)
  {
    return await _context.Bookings
        .Where(b => b.EventId == eventId)
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();
  }

  public async Task<IEnumerable<Booking>> GetPendingBookingsAsync()
  {
    return await _context.Bookings
        .Where(b => b.Status == BookingStatus.Pending)
        .OrderBy(b => b.CreatedAt)
        .ToListAsync();
  }

  public async Task<IEnumerable<Booking>> GetBookingByStatusAsync(BookingStatus status)
  {
    return await _context.Bookings
            .Where(b => b.Status == status)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync();
  }

  public async Task<Booking> CreateAsync(Booking booking)
  {
    _context.Bookings.Add(booking);
    await _context.SaveChangesAsync();
    return booking;
  }

  public async Task<Booking?> UpdateAsync(Booking booking)
  {
    var existingBooking = await _context.Bookings.FindAsync(booking.Id);
    if (existingBooking == null)
      return null;

    existingBooking.Status = booking.Status;
    existingBooking.ProcessedAt = booking.ProcessedAt;

    await _context.SaveChangesAsync();
    return existingBooking;
  }

  public async Task<bool> DeleteAsync(Guid id)
  {
    var booking = await _context.Bookings.FindAsync(id);
    if (booking == null)
      return false;

    _context.Bookings.Remove(booking);
    await _context.SaveChangesAsync();
    return true;
  }

  public async Task<bool> ExistsAsync(Guid id)
  {
    return await _context.Bookings.AnyAsync(b => b.Id == id);
  }
}