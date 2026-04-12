namespace EventManagement.DTOs;

/// <summary>
/// DTO для пагинированного результата
/// </summary>
/// <typeparam name="T">Тип элементов в результате</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Коллекция элементов на текущей странице
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();
    
    /// <summary>
    /// Общее количество элементов (без учета пагинации)
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Номер текущей страницы
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// Количество элементов на странице
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Общее количество страниц
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    /// <summary>
    /// Есть ли предыдущая страница
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
    
    /// <summary>
    /// Есть ли следующая страница
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
    
    /// <summary>
    /// Конструктор для создания пагинированного результата
    /// </summary>
    public PaginatedResult()
    {
    }
    
    /// <summary>
    /// Конструктор для создания пагинированного результата с данными
    /// </summary>
    /// <param name="items">Элементы на текущей странице</param>
    /// <param name="totalCount">Общее количество элементов</param>
    /// <param name="pageNumber">Номер текущей страницы</param>
    /// <param name="pageSize">Количество элементов на странице</param>
    public PaginatedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}