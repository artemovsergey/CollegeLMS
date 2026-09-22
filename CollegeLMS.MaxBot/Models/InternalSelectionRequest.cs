namespace CollegeLMS.MaxBot.Models;

/// <summary>Выбор группы или преподавателя, сделанный в мини-приложении (ровно одна цель).</summary>
public sealed record InternalSelectionRequest
{
    public long MaxUserId { get; init; }
    public Guid? GroupId { get; init; }
    public Guid? TeacherId { get; init; }
}
