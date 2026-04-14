using System.Net;
using System.Text.Json;
using EventManagement.Exceptions;
using EventManagement.Models;

namespace EventManagement.Middleware;

/// <summary>
/// Глобальная обработка исключений (middleware)
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

  /// <summary>
  /// middleware-обработчик ошибок
  /// </summary>
  /// <param name="next">Следующий в цепочке</param>
  /// <param name="logger">Логгер</param>
  public GlobalExceptionHandlingMiddleware(
      RequestDelegate next,
      ILogger<GlobalExceptionHandlingMiddleware> logger
      )
  {
    _next = next;
    _logger = logger;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="context">Контекст текущего HTTP-запроса</param>
  public async Task InvokeAsync(HttpContext context)
  {
    try
    {
      await _next(context);
    }
    catch (Exception ex)
    {
      await HandleExceptionAsync(context, ex);
    }
  }

  /// <summary>
  /// Обработчик исключений
  /// </summary>
  /// <param name="context">Контекст текущего HTTP-запроса</param>
  /// <param name="exception">Исключение</param>
  /// <returns>Ответ по исключению в формате JSON (Problem Detail)</returns>
  private async Task HandleExceptionAsync(HttpContext context, Exception exception)
  {
    var response = context.Response;
    response.ContentType = "application/problem+json";

    var errorResponse = new ErrorResponse
    {
      Instance = context.Request.Path,
      TraceId = context.TraceIdentifier
    };

    switch (exception)
    {
      case ValidationException validationEx:
        response.StatusCode = (int)HttpStatusCode.BadRequest;
        errorResponse.Status = response.StatusCode;
        errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        errorResponse.Title = "Validation Error";
        errorResponse.Detail = validationEx.Message;
        errorResponse.Errors = validationEx.Errors;

        _logger.LogWarning(exception,
            "Validation error occurred. Method: {Method}, Path: {Path}, Errors: {Errors}",
            context.Request.Method,
            context.Request.Path,
            JsonSerializer.Serialize(validationEx.Errors));
        break;      

      case NotFoundException notFoundEx:
        response.StatusCode = (int)HttpStatusCode.NotFound;
        errorResponse.Status = response.StatusCode;
        errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
        errorResponse.Title = "Resource Not Found";
        errorResponse.Detail = notFoundEx.Message;

        _logger.LogWarning(exception,
            "Resource not found. Method: {Method}, Path: {Path}, Message: {Message}",
            context.Request.Method,
            context.Request.Path,
            notFoundEx.Message);
        break;

      case BadRequestException badRequestEx:
        response.StatusCode = (int)HttpStatusCode.BadRequest;
        errorResponse.Status = response.StatusCode;
        errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        errorResponse.Title = "Bad Request";
        errorResponse.Detail = badRequestEx.Message;

        _logger.LogWarning(exception,
            "Bad request. Method: {Method}, Path: {Path}, Message: {Message}",
            context.Request.Method,
            context.Request.Path,
            badRequestEx.Message);
        break;

      case ArgumentException argEx:
        response.StatusCode = (int)HttpStatusCode.BadRequest;
        errorResponse.Status = response.StatusCode;
        errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        errorResponse.Title = "Invalid Argument";
        errorResponse.Detail = argEx.Message;

        _logger.LogWarning(exception,
            "Invalid argument. Method: {Method}, Path: {Path}, Message: {Message}",
            context.Request.Method,
            context.Request.Path,
            argEx.Message);
        break;

      default:
        response.StatusCode = (int)HttpStatusCode.InternalServerError;
        errorResponse.Status = response.StatusCode;
        errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
        errorResponse.Title = "Internal Server Error";
        errorResponse.Detail = exception.Message;
        errorResponse.Errors = new Dictionary<string, string[]>
        {
          ["stackTrace"] = new[] { exception.StackTrace ?? "No stack trace available" }
        };

        _logger.LogError(exception,
            "Unhandled exception occurred. Method: {Method}, Path: {Path}, TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);
        break;
    }

    var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true
    });

    await response.WriteAsync(jsonResponse);
  }
}