using EventManagement.Exceptions;

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
  /// <example>2026-05-01T00:00:00+04:00</example>
  public DateTimeOffset? From { get; set; }

  /// <summary>
  /// События, которые заканчиваются не позже указанной даты
  /// </summary>
  /// <example>2026-12-31T23:59:59Z</example>
  public DateTimeOffset? To { get; set; }

  /// <summary>
  /// Номер страницы (по умолчанию 1)
  /// </summary>
  /// <example>1</example>
  public int PageNumber { get; set; } = 1;

  /// <summary>
  /// Количество элементов на странице (по умолчанию 10)
  /// </summary>
  /// <example>10</example>
  public int PageSize { get; set; } = 10;

  /// <summary>
  /// Максимальное количество страниц
  /// </summary>
  const int maxPageSize = 10000;

  /// <summary>
  /// Валидация параметров пагинации
  /// </summary>
  public void Validate()
  {
    if (PageNumber < 1)
    {
      throw new BadRequestException("Page number must be greater than or equal to 1");
    }

    if (PageSize < 1)
    {
      throw new BadRequestException("Page size must be greater than or equal to 1");
    }

    if (PageSize > maxPageSize)
    {
      throw new BadRequestException("Page size cannot exceed {maxPageSize}");
    }
  }
}