using System.Collections;
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
    var errorResponse = new ErrorResponse { };

    switch (exception)
    {
      case ValidationException validationEx:
        errorResponse = CreateErrorResponse(context,
                                            HttpStatusCode.BadRequest,
                                            "Validation Error",
                                            validationEx.Message,
                                            validationEx.Errors);
        _logger.LogWarning(exception,
            "Validation error occurred. Method: {Method}, Path: {Path}, Errors: {Errors}",
            context.Request.Method,
            context.Request.Path,
            JsonSerializer.Serialize(validationEx.Errors));
        break;

      case NotFoundException notFoundEx:
        errorResponse = CreateErrorResponse(context,
                                            HttpStatusCode.NotFound,
                                            "Resource Not Found",
                                            notFoundEx.Message);
        _logger.LogWarning(exception,
            "Resource not found. Method: {Method}, Path: {Path}, Message: {Message}",
            context.Request.Method,
            context.Request.Path,
            notFoundEx.Message);
        break;

      case BadRequestException badRequestEx:
        errorResponse = CreateErrorResponse(context,
                                            HttpStatusCode.BadRequest,
                                            "Bad Request",
                                            badRequestEx.Message);
        _logger.LogWarning(exception,
            "Bad request. Method: {Method}, Path: {Path}, Message: {Message}",
            context.Request.Method,
            context.Request.Path,
            badRequestEx.Message);
        break;

      case ArgumentException argEx:
        errorResponse = CreateErrorResponse(context,
                                            HttpStatusCode.BadRequest,
                                            "Invalid Argument",
                                            argEx.Message);
        _logger.LogWarning(exception,
            "Invalid argument. Method: {Method}, Path: {Path}, Message: {Message}",
            context.Request.Method,
            context.Request.Path,
            argEx.Message);
        break;

      default:
        errorResponse = CreateErrorResponse(context,
                                            HttpStatusCode.InternalServerError,
                                            "Internal Server Error",
                                            exception.Message,
                                            new Dictionary<string, string[]>
                                            {
                                              ["stackTrace"] = new[] { exception.StackTrace ?? "No stack trace available" }
                                            });
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

  /// <summary>
  /// Создать объект ErrorResponse в формате Problem Details (RFC 7807)
  /// </summary>
  /// <param name="context">Контекст текущего http-запроса</param>
  /// <param name="statusCode">Статус код (RFC2616)</param>
  /// <param name="title">Заголовок ошибки</param>
  /// <param name="detail">Детальное описание ошибки</param>
  /// <param name="errors">Расширенное описание ошибки</param>
  /// <returns>Ответа об ошибке в формате Problem Details (RFC 7807)</returns>
  private ErrorResponse CreateErrorResponse(HttpContext context,
                                            HttpStatusCode statusCode,
                                            string title,
                                            string detail,
                                            IDictionary<string, string[]>? errors = null)
  {
    var errorResponse = new ErrorResponse
    {
      Instance = context.Request.Path,
      TraceId = context.TraceIdentifier
    };

    switch (statusCode)
    {
      case HttpStatusCode.BadRequest:
        errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        break;
      case HttpStatusCode.NotFound:
        errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
        break;
      case HttpStatusCode.InternalServerError:
        errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
        break;
      default:
        errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
        break;
    }

    errorResponse.Status = (int)statusCode;
    errorResponse.Title = title;
    errorResponse.Detail = detail;
    errorResponse.Errors = errors;

    return errorResponse;
  }
}