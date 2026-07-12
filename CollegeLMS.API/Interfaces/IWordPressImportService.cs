using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IWordPressImportService
{
    Task<Result<ImportResult>> ImportFromJsonAsync(string jsonPath, CancellationToken ct, string? importId = null);

    string StartImport(Func<CancellationToken, Task> importAction);

    ImportProgressDto? GetImportProgress(string importId);

    Task<Result<ImportResult>> ImportFromRestApiAsync(string baseUrl, CancellationToken ct, string? importId = null);
}

public record ImportResult(
    int CategoriesCreated,
    int PostsImported,
    int PostsSkipped,
    List<string> Errors
);
