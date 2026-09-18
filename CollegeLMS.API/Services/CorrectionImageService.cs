using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
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
                page.DefaultTextStyle(t => t.FontSize(11).FontColor("#111827"));

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
                                            .Background("#1e3a5f")
                                            .Padding(5)
                                            .Text(title)
                                            .FontColor(Colors.White)
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
                                            .BorderColor("#e5e7eb")
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
        changeType switch
        {
            ScheduleChangeType.Add => "Добавлено",
            ScheduleChangeType.Remove => "Снято",
            ScheduleChangeType.Replace => "Замена",
            ScheduleChangeType.Move => "Перенос",
            _ => changeType.ToString(),
        };

    private static string PairLabel(CorrectionPosition position)
    {
        if (
            position.ChangeType is ScheduleChangeType.Replace or ScheduleChangeType.Move
            && position.RemovedNumberPair.HasValue
            && position.RemovedNumberPair != position.NumberPair
        )
        {
            return $"{position.RemovedNumberPair} → {position.NumberPair}";
        }

        return position.NumberPair.ToString();
    }

    private static (string Removed, string Added) Describe(CorrectionPosition position)
    {
        var removed = position.ChangeType switch
        {
            ScheduleChangeType.Remove => Combine(
                position.RemovedSubject ?? position.Subject,
                position.RemovedTeacherName ?? position.TeacherName
            ),
            ScheduleChangeType.Replace or ScheduleChangeType.Move => Combine(
                position.RemovedSubject,
                position.RemovedTeacherName
            ),
            _ => "—",
        };

        var added = position.ChangeType switch
        {
            ScheduleChangeType.Add or ScheduleChangeType.Replace or ScheduleChangeType.Move =>
                Combine(position.Subject, position.TeacherName),
            _ => "—",
        };

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
