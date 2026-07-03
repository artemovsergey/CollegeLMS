namespace CollegeLMS.API.Dtos;

public class CreateCourseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public Guid? TeacherId { get; set; }
}

public class UpdateCourseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public string Status { get; set; } = string.Empty;
}
