namespace EventManagement.DTOs;

/// <summary>
/// DTO для параметров фильтрации мероприятий
/// </summary>
public class EventFilterDto
{
    /// <summary>
    /// Поиск по названию (регистронезависимый, частичное совпадение)
    /// </summary>
    /// <example>Conference</example>
    public string? Title { get; set; }
    
    /// <summary>
    /// События, которые начинаются не раньше указанной даты
    /// </summary>
    /// <example>2026-05-01T00:00:00Z</example>
    public DateTime? From { get; set; }
    
    /// <summary>
    /// События, которые заканчиваются не позже указанной даты
    /// </summary>
    /// <example>2026-12-31T23:59:59Z</example>
    public DateTime? To { get; set; }
}