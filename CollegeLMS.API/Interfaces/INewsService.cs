using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface INewsService
{
    Task<Result<PagedResponse<NewsResponse>>> GetAllAsync(
        int page,
        int pageSize,
        Guid? categoryId,
        string? search,
        CancellationToken ct = default
    );
    Task<Result<NewsResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<NewsResponse>> CreateAsync(
        CreateNewsRequest request,
        Guid currentUserId,
        CancellationToken ct = default
    );
    Task<Result<NewsResponse>> UpdateAsync(
        Guid id,
        UpdateNewsRequest request,
        CancellationToken ct = default
    );
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<List<NewsCategoryResponse>>> GetCategoriesAsync(CancellationToken ct = default);
}
