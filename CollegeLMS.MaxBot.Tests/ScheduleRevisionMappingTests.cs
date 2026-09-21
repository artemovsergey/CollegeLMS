using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Models;

namespace CollegeLMS.MaxBot.Tests;

public class ScheduleRevisionMappingTests
{
    [Fact]
    public void FromNotify_UnspecifiedCorrectionDate_StoresUtcKind()
    {
        var change = BuildChange(new DateTime(2026, 9, 22));

        var revision = ScheduleRevision.FromNotify(change, DateTime.UtcNow);

        revision.CorrectionDate.Should().NotBeNull();
        revision.CorrectionDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
        revision.CorrectionDate!.Value.Date.Should().Be(new DateTime(2026, 9, 22));
    }

    [Fact]
    public void FromNotify_NullCorrectionDate_StaysNull()
    {
        var revision = ScheduleRevision.FromNotify(BuildChange(null), DateTime.UtcNow);

        revision.CorrectionDate.Should().BeNull();
    }

    [Fact]
    public void FromNotify_KeepsFieldsAndUtcCreatedAt()
    {
        var now = DateTime.UtcNow;

        var revision = ScheduleRevision.FromNotify(BuildChange(new DateTime(2026, 9, 22)), now);

        revision.ChangeType.Should().Be("Add");
        revision.GroupName.Should().Be("ПО262");
        revision.Subject.Should().Be("История");
        revision.Room.Should().BeEmpty();
        revision.DayOfWeek.Should().Be("Вторник");
        revision.Week.Should().Be(3);
        revision.NumberPair.Should().Be(2);
        revision.CreatedAt.Should().Be(now);
        revision.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    private static NotifyChangeDto BuildChange(DateTime? correctionDate) =>
        new()
        {
            Id = Guid.NewGuid(),
            ChangeType = "Add",
            GroupName = "ПО262",
            DayOfWeek = 2,
            Week = 3,
            NumberPair = 2,
            Subject = "История",
            CorrectionDate = correctionDate,
        };
}
