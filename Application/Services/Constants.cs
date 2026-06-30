namespace EventManagement.Application.Services;

/// <summary>
/// Константы
/// </summary>
public static class Constants
{
  /// <summary>
  /// Период опроса броней
  /// </summary>
  public static readonly TimeSpan processingInterval;
  /// <summary>
  /// Задержка имитации обращения к внешней системе
  /// </summary>
  public static readonly TimeSpan processingDelay;
  static Constants()
  {
    processingInterval = new TimeSpan(0, 0, 5);
    processingDelay = new TimeSpan(0, 0, 2);
  }    
}