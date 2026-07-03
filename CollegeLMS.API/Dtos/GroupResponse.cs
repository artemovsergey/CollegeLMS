namespace CollegeLMS.API.Dtos;

public class GroupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }
    public int StudentCount { get; set; }
}
