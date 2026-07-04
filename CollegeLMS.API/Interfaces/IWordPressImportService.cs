using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IWordPressImportService
{
    Task<Result<ImportResult>> ImportFromJsonAsync(string jsonPath, CancellationToken ct);
}

public record ImportResult(
    int CategoriesCreated,
    int PostsImported,
    int PostsSkipped,
    List<string> Errors
);
