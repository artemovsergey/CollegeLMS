namespace CollegeLMS.API.Dtos;

public class CreateLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Kind { get; set; } = "Lecture";
    public Guid? TestId { get; set; }
}

public class UpdateLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Kind { get; set; } = "Lecture";
    public Guid? TestId { get; set; }
}