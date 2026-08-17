namespace CollegeLMS.API.Dtos;

public class CreateCourseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? TeacherId { get; set; }
    public List<Guid> AuthorIds { get; set; } = new();
}

public class UpdateCourseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<Guid> AuthorIds { get; set; } = new();
}
