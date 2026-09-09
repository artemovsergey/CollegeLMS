using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Services;

public class DocumentsService(IConfiguration config) : IDocumentsService
{
    private readonly string _templatesPath =
        config["TemplatesPath"] ?? Path.Combine("..", "import", "schedule");

    private static readonly (string FileName, string Name, string Description)[] Known =
    [
        ("Расписание.xlsx", "Расписание", "Шаблон расписания для импорта"),
        ("Корректировка.xlsx", "Корректировка", "Шаблон корректировки расписания для импорта"),
    ];

    public Task<Result<List<DocumentTemplateResponse>>> GetTemplatesAsync(CancellationToken ct)
    {
        var result = Known
            .Select(k => new DocumentTemplateResponse
            {
                FileName = k.FileName,
                Name = k.Name,
                Description = k.Description,
                Size = File.Exists(Path.Combine(_templatesPath, k.FileName))
                    ? new FileInfo(Path.Combine(_templatesPath, k.FileName)).Length
                    : 0,
            })
            .ToList();

        return Task.FromResult(Result<List<DocumentTemplateResponse>>.Ok(result));
    }

    public async Task<Result<DocumentDownloadResult>> DownloadAsync(
        string fileName,
        CancellationToken ct
    )
    {
        var known = Known.FirstOrDefault(k => k.FileName == fileName);
        if (known.FileName is null)
            return Result<DocumentDownloadResult>.Fail("Шаблон не найден", 404);

        var path = Path.GetFullPath(Path.Combine(_templatesPath, fileName));
        if (!File.Exists(path))
            return Result<DocumentDownloadResult>.Fail("Файл шаблона отсутствует на сервере", 404);

        var content = await File.ReadAllBytesAsync(path, ct);
        return Result<DocumentDownloadResult>.Ok(
            new DocumentDownloadResult { Content = content, FileName = fileName }
        );
    }
}
