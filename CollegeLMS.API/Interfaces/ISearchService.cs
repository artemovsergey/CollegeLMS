using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface ISearchService
{
    Task<Result<PagedResponse<SearchResponse>>> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
}
