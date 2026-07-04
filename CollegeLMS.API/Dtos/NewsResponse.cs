namespace CollegeLMS.API.Dtos;

public class NewsResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsPublished { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}
