using System.Text.Json.Serialization;

namespace EventManagement.Models;

/// <summary>
/// Модель ответа об ошибке в формате Problem Details (RFC 7807)
/// </summary>
public class ErrorResponse
{
  /// <summary>
  /// Тип ошибки (URI)
  /// </summary>
  [JsonPropertyName("type")]
  public string Type { get; set; } = string.Empty;

  /// <summary>
  /// Заголовок ошибки
  /// </summary>
  [JsonPropertyName("title")]
  public string Title { get; set; } = string.Empty;

  /// <summary>
  /// HTTP статус код
  /// </summary>
  [JsonPropertyName("status")]
  public int Status { get; set; }

  /// <summary>
  /// Детальное описание ошибки
  /// </summary>
  [JsonPropertyName("detail")]
  public string Detail { get; set; } = string.Empty;

  /// <summary>
  /// Путь к запросу, вызвавшему ошибку (URI)
  /// </summary>
  [JsonPropertyName("instance")]
  public string Instance { get; set; } = string.Empty;

  /// === Расширенные данные ответа об ошибке ===

  /// <summary>
  /// Расширенные данные об ошибке
  /// </summary>
  [JsonPropertyName("errors")]
  public IDictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// Trace ID для отслеживания запроса
    /// </summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

}