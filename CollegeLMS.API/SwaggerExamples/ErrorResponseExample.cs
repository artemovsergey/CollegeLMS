using CollegeLMS.API.Response;

namespace CollegeLMS.API.SwaggerExamples;

/// <summary>Примеры ответов с ошибками.</summary>
public static class ErrorResponseExample
{
    public static ErrorResponse NotFound(string entity = "Запись") =>
        new() { StatusCode = 404, Message = $"{entity} не найдена" };

    public static ErrorResponse ValidationError() =>
        new()
        {
            StatusCode = 400,
            Message = "Ошибка валидации",
            Detail = "{\"Email\": [\"Поле обязательно\"]}",
        };

    public static ErrorResponse Unauthorized() =>
        new() { StatusCode = 401, Message = "Не авторизован" };

    public static ErrorResponse Forbidden() =>
        new() { StatusCode = 403, Message = "Доступ запрещён" };

    public static ErrorResponse ServerError() =>
        new() { StatusCode = 500, Message = "Внутренняя ошибка сервера" };
}
