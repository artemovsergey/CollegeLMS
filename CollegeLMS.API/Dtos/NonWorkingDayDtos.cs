namespace CollegeLMS.API.Dtos;

public class NonWorkingDayResponse
{
    public Guid Id { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class NonWorkingDayRequest
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string Title { get; set; } = string.Empty;
}
