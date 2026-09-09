using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IDocumentsService
{
    Task<Result<List<DocumentTemplateResponse>>> GetTemplatesAsync(CancellationToken ct);
    Task<Result<DocumentDownloadResult>> DownloadAsync(string fileName, CancellationToken ct);
}

public class DocumentDownloadResult
{
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string FileName { get; set; } = string.Empty;
}