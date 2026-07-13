using Microsoft.EntityFrameworkCore;
using EventManagement.Application.Ports;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Enums;
using EventManagement.Infrastructure.Data;

namespace EventManagement.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    private readonly int _maxActiveBookings = 10;
    
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<User?> GetByIdAsync(Guid id)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    
    public async Task<User?> GetByLoginAsync(string login)
        => await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
    
    public async Task<IEnumerable<User>> GetAllAsync()
        => await _context.Users.ToListAsync();
    
    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
    
    public async Task<User?> UpdateAsync(User user)
    {
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (existing == null) return null;
        
        existing.UpdatePassword(user.PasswordHash);
        await _context.SaveChangesAsync();
        return existing;
    }
    
    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return false;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountActiveBookingsAsync(Guid userId)
    {
        return await _context.Bookings
            .CountAsync(b => b.UserId == userId && 
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed));
    }    
}