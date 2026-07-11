namespace CollegeLMS.API.Dtos;

public enum ExportFormat
{
    Pdf,
    Xlsx,
}

public class ExportResult
{
    public byte[] FileContent { get; set; } = [];
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
