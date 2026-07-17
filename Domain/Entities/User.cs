using EventManagement.Domain.Enums;

namespace EventManagement.Domain.Entities;

public class User
{
    /// <summary>
    /// Пользователь
    /// </summary>
    private User() { }
    
    public User(string login, string passwordHash, Role role)
    {
        Id = Guid.NewGuid();
        Login = login ?? throw new ArgumentNullException(nameof(login));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        Role = role;
        Bookings = new List<Booking>();
    }
    
    public Guid Id { get; private set; }
    public string Login { get; private set; }
    public string PasswordHash { get; private set; }
    public Role Role { get; private set; }
    public ICollection<Booking> Bookings { get; private set; }
    
    public bool IsAdmin() => Role == Role.Admin;
    
    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash ?? throw new ArgumentNullException(nameof(newPasswordHash));
    }
}