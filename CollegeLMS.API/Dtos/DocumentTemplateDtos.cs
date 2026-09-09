namespace CollegeLMS.API.Dtos;

public class DocumentTemplateResponse
{
    public string FileName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long Size { get; set; }
}
