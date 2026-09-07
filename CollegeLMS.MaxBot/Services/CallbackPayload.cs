using System.Globalization;

namespace CollegeLMS.MaxBot.Services;

public sealed record CallbackPayload(string Action, string? Param1, string? Param2)
{
    public static CallbackPayload? Parse(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        var parts = payload.Split(':', 3);
        return new CallbackPayload(
            parts[0],
            parts.Length > 1 ? parts[1] : null,
            parts.Length > 2 ? parts[2] : null
        );
    }

    public static string Day(DateTime date) => $"day:{date:yyyy-MM-dd}";
    public static string DayPrev(DateTime date) => $"dayprev:{date:yyyy-MM-dd}";
    public static string DayNext(DateTime date) => $"daynext:{date:yyyy-MM-dd}";
    public static string Week(DateTime date) => $"week:{date:yyyy-MM-dd}";
    public static string WeekPrev(DateTime date) => $"weekprev:{date:yyyy-MM-dd}";
    public static string WeekNext(DateTime date) => $"weeknext:{date:yyyy-MM-dd}";
    public static string Cal(DateTime month) => $"cal:{month:yyyy-MM}";
    public static string CalPrev(DateTime month) => $"calprev:{month:yyyy-MM}";
    public static string CalNext(DateTime month) => $"calnext:{month:yyyy-MM}";

    public static DateTime? TryParseDate(string? text)
    {
        if (text is null)
            return null;

        return DateTime.TryParseExact(
            text,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date
        )
            ? date
            : null;
    }

    public static DateTime? TryParseMonth(string? text)
    {
        if (text is null)
            return null;

        return DateTime.TryParseExact(
            text,
            "yyyy-MM",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var month
        )
            ? month
            : null;
    }
}