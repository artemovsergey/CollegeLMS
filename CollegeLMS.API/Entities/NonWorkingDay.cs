namespace CollegeLMS.API.Entities;

/// <summary>Нерабочая дата (выходной или праздник).</summary>
public class NonWorkingDay : Entity
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string Title { get; set; } = string.Empty;
}
