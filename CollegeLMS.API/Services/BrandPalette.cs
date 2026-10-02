namespace CollegeLMS.API.Services;

/// <summary>
/// Палитра CollegeLMS для мест, где токены темы недоступны.
///
/// Источник истины — <c>CollegeLMS.Next/app/globals.css</c> (§3.1–3.2).
/// Значения здесь повторяют светлую тему: PNG-корректировка печатается и
/// публикуется в чат, где нет ни CSS, ни переключателя темы, поэтому тёмная
/// тема для неё не существует.
///
/// Раньше литералы брались из палитры Tailwind: шапка таблицы была
/// <c>#1e3a5f</c> (blue-900) и <c>#e5e7eb</c> (gray-200) — то есть не
/// брендовый тёмно-синий <c>#24386a</c> и не граница <c>#8a93a5</c>. Картинка
/// корректировки уходила в мессенджер брендового цвета, а всё остальное
/// расписание — нет.
/// </summary>
public static class BrandPalette
{
    /// <summary>Основной текст. Совпадает с <c>--fg</c>.</summary>
    public const string Fg = "#111827";

    /// <summary>Брендовый тёмно-синий. Совпадает с <c>--accent</c>.</summary>
    public const string Accent = "#24386a";

    /// <summary>Текст на брендовой заливке. Совпадает с <c>--accent-foreground</c>.</summary>
    public const string AccentForeground = "#ffffff";

    /// <summary>Граница. Совпадает с <c>--border</c>.</summary>
    public const string Border = "#8a93a5";

    /// <summary>Приглушённый текст. Совпадает с <c>--muted-fg</c>.</summary>
    public const string MutedForeground = "#4a5a7a";

    /// <summary>Приглушённая поверхность. Совпадает с <c>--muted</c>.</summary>
    public const string Muted = "#eef0f4";

    /// <summary>Ошибка. Совпадает с <c>--destructive</c>.</summary>
    public const string Destructive = "#dc2626";
}