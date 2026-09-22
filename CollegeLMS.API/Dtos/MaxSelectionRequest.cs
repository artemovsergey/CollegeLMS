namespace CollegeLMS.API.Dtos;

/// <summary>Выбор группы или преподавателя, сделанный в мини-приложении.</summary>
public class MaxSelectionRequest
{
    public Guid? GroupId { get; set; }
    public Guid? TeacherId { get; set; }
}
