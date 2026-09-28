using ClosedXML.Excel;

namespace CollegeLMS.API.Services;

/// <summary>
/// Общие настройки выгружаемых XLSX. Тема, которую ClosedXML подставляет по
/// умолчанию, — Calibri, а документы колледжа набраны Times New Roman.
/// <para>
/// Шрифт задаётся явно на заполненную область листа: значение по умолчанию
/// книги не переживает сериализацию, поэтому полагаться на него нельзя —
/// иначе пустые и не помеченные ячейки остались бы в Calibri.
/// </para>
/// </summary>
internal static class XlsxDefaults
{
    public const string FontName = "Times New Roman";

    /// <summary>Проставляет Times New Roman на все использованные ячейки листа.</summary>
    public static void ApplyFont(IXLWorksheet sheet)
    {
        var used = sheet.RangeUsed();
        if (used is not null)
            used.Style.Font.FontName = FontName;
    }
}
