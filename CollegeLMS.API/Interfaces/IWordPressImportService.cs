using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IWordPressImportService
{
    Task<Result<ImportResult>> ImportFromJsonAsync(
        string jsonPath,
        CancellationToken ct,
        Guid? jobId = null
    );

    string StartImport(Func<CancellationToken, Task> importAction);

    void StopImport(string importId);

    Task<ImportProgressDto?> GetImportProgressAsync(string importId, CancellationToken ct);
    Task<ImportProgressDto?> GetActiveImportAsync(CancellationToken ct);

    Task<Result<ImportResult>> ImportFromRestApiAsync(
        string baseUrl,
        CancellationToken ct,
        Guid? jobId = null
    );
}

public record ImportResult(
    int CategoriesCreated,
    int PostsImported,
    int PostsSkipped,
    List<string> Errors
);
