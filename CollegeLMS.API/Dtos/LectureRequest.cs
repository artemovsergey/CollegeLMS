namespace CollegeLMS.API.Dtos;

public class CreateLectureRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string LectureType { get; set; } = "Lecture";
    public Guid? TestId { get; set; }
}

public class UpdateLectureRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string LectureType { get; set; } = "Lecture";
    public Guid? TestId { get; set; }
}
