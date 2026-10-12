using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.Shared;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CollegeLMS.API.Services;

/// <summary>
/// Рендер PNG-таблицы корректировки для канала Max.
/// Использует QuestPDF (встроенный шрифт Lato с кириллицей).
/// </summary>
public class CorrectionImageService
{
    private static readonly string[] DayNames =
    [
        "",
        "Понедельник",
        "Вторник",
        "Среда",
        "Четверг",
        "Пятница",
        "Суббота",
        "Воскресенье",
    ];

    public byte[] Render(CorrectionBatch batch)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var positions = batch.Positions.OrderBy(p => p.Row).ToList();
        var rowCount = Math.Max(positions.Count, 1);
        var height = 130 + rowCount * 38;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(new PageSize(860, height));
                page.Margin(24);
                page.DefaultTextStyle(t => t.FontSize(11).FontColor(BrandPalette.Fg));

                page.Content()
                    .Column(column =>
                    {
                        column.Spacing(12);

                        column
                            .Item()
                            .Text(
                                $"Корректировка расписания на {batch.CorrectionDate:dd.MM.yyyy} ({DayName(batch.CorrectionDate.DayOfWeek)})"
                            )
                            .FontSize(16)
                            .SemiBold();

                        column
                            .Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(55);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.ConstantColumn(120);
                                });

                                table.Header(header =>
                                {
                                    foreach (
                                        var title in new[]
                                        {
                                            "Группа",
                                            "Тип",
                                            "Пара",
                                            "Снимается",
                                            "Вводится",
                                            "Примечание",
                                        }
                                    )
                                    {
                                        header
                                            .Cell()
                                            .Background(BrandPalette.Accent)
                                            .Padding(5)
                                            .Text(title)
                                            .FontColor(BrandPalette.AccentForeground)
                                            .FontSize(10)
                                            .SemiBold();
                                    }
                                });

                                foreach (var position in positions)
                                {
                                    var (removedText, addedText) = Describe(position);

                                    foreach (
                                        var text in new[]
                                        {
                                            position.GroupName,
                                            ChangeTypeLabel(position.ChangeType),
                                            PairLabel(position),
                                            removedText,
                                            addedText,
                                            string.IsNullOrWhiteSpace(position.Note)
                                                ? "—"
                                                : position.Note,
                                        }
                                    )
                                    {
                                        table
                                            .Cell()
                                            .BorderBottom(0.5f)
                                            .BorderColor(BrandPalette.Border)
                                            .PaddingVertical(5)
                                            .PaddingHorizontal(5)
                                            .Text(text)
                                            .FontSize(10);
                                    }
                                }
                            });
                    });
            });
        });

        var settings = new ImageGenerationSettings
        {
            ImageFormat = ImageFormat.Png,
            ImageCompressionQuality = ImageCompressionQuality.High,
            RasterDpi = 144,
        };

        using var images = document.GenerateImages(settings).GetEnumerator();
        return images.MoveNext() ? images.Current : [];
    }

    private static string DayName(DayOfWeek day) => DayNames[(int)day];

    private static string ChangeTypeLabel(ScheduleChangeType changeType) =>
        ChangeVocabulary.Label(changeType.ToString());

    private static string PairLabel(CorrectionPosition position)
    {
        if (CorrectionMoves.FromPair(position) is { } from)
        {
            return $"{from} {ChangeVocabulary.ChangeArrow} {position.NumberPair}";
        }

        return position.NumberPair.ToString();
    }

    internal static (string Removed, string Added) Describe(CorrectionPosition position)
    {
        var isRemove = position.ChangeType == ScheduleChangeType.Remove;

        // При чистом переносе снимать нечего: вводимое занятие само уезжает из
        // пары «откуда», а пара ввода не трогается. Колонка «снимается» остаётся
        // пустой — как в файле, из которого позиция и пришла. Пара «откуда» и
        // так видна в колонке «пара» как «откуда => куда».
        var removed =
            CorrectionMoves.IsPureMove(position) ? "—"
            : isRemove
                ? Combine(
                    position.RemovedSubject ?? position.Subject,
                    position.RemovedTeacherName ?? position.TeacherName
                )
            : Combine(position.RemovedSubject, position.RemovedTeacherName);

        var added = isRemove ? "—" : Combine(position.Subject, position.TeacherName);

        return (removed, added);
    }

    private static string Combine(string? subject, string? teacher)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(subject))
            parts.Add(subject);
        if (!string.IsNullOrWhiteSpace(teacher))
            parts.Add(teacher);

        return parts.Count == 0 ? "—" : string.Join("\n", parts);
    }
}
