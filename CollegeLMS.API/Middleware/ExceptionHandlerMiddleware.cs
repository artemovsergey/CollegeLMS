using System.Net;
using System.Text.Json;
using CollegeLMS.API.Exceptions;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CollegeLMS.API.Middleware;

public class ExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlerMiddleware> logger
)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            NotFoundException => ((int)HttpStatusCode.NotFound, ex.Message),
            ValidationException => ((int)HttpStatusCode.BadRequest, ex.Message),
            ArgumentException => ((int)HttpStatusCode.BadRequest, ex.Message),
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "Не авторизован"),
            NpgsqlException or DbUpdateException => (
                (int)HttpStatusCode.InternalServerError,
                "Ошибка базы данных"
            ),
            _ => ((int)HttpStatusCode.InternalServerError, "Внутренняя ошибка сервера"),
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = message,
            Detail = context
                .RequestServices.GetRequiredService<IWebHostEnvironment>()
                .IsDevelopment()
                ? ex.ToString()
                : null,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
