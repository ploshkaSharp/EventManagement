namespace EventManagement.Models;

/// <summary>
/// Мероприятие
/// </summary>
public class Event
{
  // Приватный конструктор для EF Core
  private Event()
  {
    Title = null!;
    Bookings = new List<Booking>();
  }
  
  /// <summary>
  /// 
  /// </summary>
  /// <param name="title">Название мероприятия</param>
  /// <param name="startAt">Дата и время начала мероприятия</param>
  /// <param name="endAt">Дата и время окончания мероприятия</param>
  public Event(string title, DateTimeOffset startAt, DateTimeOffset endAt)
  {
    Id = Guid.NewGuid();
    Title = title;
    StartAt = startAt;
    EndAt = endAt;
    Bookings = new List<Booking>();
  }

  /// <summary>
  /// Уникальный идентификатор мероприятия
  /// </summary>
  /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>  
  public Guid Id { get; set; }
  /// <summary>
  /// Название мероприятия
  /// </summary>
  /// <example>Tech Conference 2026</example>
  public string Title { get; set; } = string.Empty;
  /// <summary>
  /// Описание мероприятия
  /// </summary>
  /// <example>Annual technology conference with industry experts</example>
  public string? Description { get; set; }
  /// <summary>
  /// Дата и время начала мероприятия
  /// </summary>
  /// <example>2026-05-15T10:00:00+04:00</example>
  public DateTimeOffset StartAt { get; set; }
  /// <summary>
  /// Дата и время окончания мероприятия
  /// </summary>
  /// <example>2026-05-15T18:00:00+04:00</example>
  public DateTimeOffset EndAt { get; set; }
  /// <summary>
  /// Общее количество мест на событии
  /// </summary>
  public int TotalSeats { get; set; }
  /// <summary>
  /// Текущее количество свободных мест
  /// </summary>
  public int AvailableSeats { get; set; }
  /// <summary>
  /// Навигационное свойство 
  /// </summary>
  public ICollection<Booking> Bookings { get; private set; }

  /// <summary>
  /// Попытка забронировать места
  /// </summary>
  /// <param name="count">Количество мест для бронирования</param>
  /// <returns>True если места успешно забронированы, иначе False</returns>
  public bool TryReserveSeats(int count = 1)
  {
    if (AvailableSeats >= count)
    {
      AvailableSeats -= count;
      return true;
    }
    return false;
  }

  /// <summary>
  /// Освободить места
  /// </summary>
  /// <param name="count">Количество мест для освобождения</param>
  public void ReleaseSeats(int count = 1)
  {
    AvailableSeats += count;
    if (AvailableSeats > TotalSeats)
    {
      AvailableSeats = TotalSeats;
    }
  }
}
