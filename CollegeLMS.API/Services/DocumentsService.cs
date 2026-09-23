using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Services;

public class DocumentsService(IConfiguration config) : IDocumentsService
{
    private const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private readonly string _schedulePath =
        config["TemplatesPath"] ?? Path.Combine("..", "import", "schedule");

    private readonly string _practicePath =
        config["PracticeTemplatesPath"] ?? Path.Combine("..", "import", "templates");

    private (
        string FileName,
        string Name,
        string Description,
        string Directory,
        string ContentType
    )[] Known =>
        [
            (
                "Расписание.xlsx",
                "Расписание",
                "Шаблон расписания для импорта",
                _schedulePath,
                XlsxContentType
            ),
            (
                "Корректировка.xlsx",
                "Корректировка",
                "Шаблон корректировки расписания для импорта",
                _schedulePath,
                XlsxContentType
            ),
            (
                "Шаблон графика УП.docx",
                "График УП",
                "Шаблон графика учебной практики для импорта и экспорта",
                _practicePath,
                DocxContentType
            ),
        ];

    public Task<Result<List<DocumentTemplateResponse>>> GetTemplatesAsync(CancellationToken ct)
    {
        var result = Known
            .Select(k =>
            {
                var path = Path.Combine(k.Directory, k.FileName);
                return new DocumentTemplateResponse
                {
                    FileName = k.FileName,
                    Name = k.Name,
                    Description = k.Description,
                    Size = File.Exists(path) ? new FileInfo(path).Length : 0,
                };
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

        var path = Path.GetFullPath(Path.Combine(known.Directory, fileName));
        if (!File.Exists(path))
            return Result<DocumentDownloadResult>.Fail("Файл шаблона отсутствует на сервере", 404);

        var content = await File.ReadAllBytesAsync(path, ct);
        return Result<DocumentDownloadResult>.Ok(
            new DocumentDownloadResult
            {
                Content = content,
                ContentType = known.ContentType,
                FileName = fileName,
            }
        );
    }
}
