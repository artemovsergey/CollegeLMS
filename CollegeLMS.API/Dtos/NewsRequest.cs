namespace CollegeLMS.API.Dtos;

public class CreateNewsRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class UpdateNewsRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
}
