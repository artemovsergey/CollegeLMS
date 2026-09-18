using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

/// <summary>Рендер PNG-картинки корректировки для канала Max (UC-SCH-28/29).</summary>
public class CorrectionImageServiceTests
{
    private static CorrectionBatch BuildBatch()
    {
        var utcNow = DateTime.UtcNow;
        var batch = new CorrectionBatch
        {
            Id = Guid.NewGuid(),
            CorrectionDate = new DateTime(2026, 9, 8),
            Week = 2,
            DayOfWeek = (int)DayOfWeek.Tuesday,
            Status = CorrectionBatchStatus.Draft,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        batch.Positions.Add(
            new CorrectionPosition
            {
                Id = Guid.NewGuid(),
                BatchId = batch.Id,
                Row = 1,
                ChangeType = ScheduleChangeType.Replace,
                GroupId = Guid.NewGuid(),
                GroupName = "ПО-262",
                DayOfWeek = (int)DayOfWeek.Tuesday,
                Week = 2,
                NumberPair = 3,
                Subject = "Математика",
                TeacherName = "Марченко И.А.",
                RemovedSubject = "Физика",
                RemovedTeacherName = "Иванов И.И.",
                RemovedNumberPair = 3,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        return batch;
    }

    [Fact]
    public void Render_ReturnsNonEmptyPng()
    {
        var service = new CorrectionImageService();

        var png = service.Render(BuildBatch());

        png.Should().NotBeEmpty();
        png.Take(4).Should().Equal(0x89, 0x50, 0x4E, 0x47);
    }

    [Fact]
    public void Render_EmptyBatch_StillReturnsPng()
    {
        var batch = BuildBatch();
        batch.Positions.Clear();
        var service = new CorrectionImageService();

        var png = service.Render(batch);

        png.Should().NotBeEmpty();
        png.Take(4).Should().Equal(0x89, 0x50, 0x4E, 0x47);
    }
}
