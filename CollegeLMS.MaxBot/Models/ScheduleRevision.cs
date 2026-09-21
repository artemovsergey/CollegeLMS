using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Models;

public class ScheduleRevision
{
    public long Id { get; set; }

    public Guid ForeignId { get; set; }

    public string ChangeType { get; set; } = "";

    public string GroupName { get; set; } = "";

    public string? TeacherName { get; set; }

    public string Subject { get; set; } = "";

    public string Room { get; set; } = "";

    public string DayOfWeek { get; set; } = "";

    public int Week { get; set; }

    public int NumberPair { get; set; }

    public string? Note { get; set; }

    public string? RemovedSubject { get; set; }

    public string? RemovedTeacherName { get; set; }

    public int? RemovedNumberPair { get; set; }

    public DateTime? CorrectionDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Создаёт ревизию из уведомления API. Даты нормализуются к UTC:
    /// Npgsql не принимает DateTime Kind=Unspecified для timestamptz.
    /// </summary>
    public static ScheduleRevision FromNotify(NotifyChangeDto change, DateTime now) =>
        new()
        {
            ForeignId = change.Id,
            ChangeType = change.ChangeType,
            GroupName = change.GroupName,
            TeacherName = change.TeacherName,
            Subject = change.Subject,
            Room = "",
            DayOfWeek = MessageFormatter.GetDayLabel(MessageFormatter.DayIndex(change.DayOfWeek)),
            Week = change.Week,
            NumberPair = change.NumberPair,
            Note = change.Note,
            RemovedSubject = change.RemovedSubject,
            RemovedTeacherName = change.RemovedTeacherName,
            RemovedNumberPair = change.RemovedNumberPair,
            CorrectionDate = ToUtc(change.CorrectionDate),
            CreatedAt = now,
        };

    private static DateTime? ToUtc(DateTime? value) =>
        value switch
        {
            null => null,
            { Kind: DateTimeKind.Local } v => v.ToUniversalTime(),
            { } v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
        };
}
