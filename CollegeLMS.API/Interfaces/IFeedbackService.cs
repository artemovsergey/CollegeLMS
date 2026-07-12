using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IFeedbackService
{
    Task<Result<FeedbackResponse>> CreateAsync(
        FeedbackRequest request,
        CancellationToken ct = default
    );

    Task<Result<List<FeedbackListItemDto>>> GetAllAsync(CancellationToken ct = default);
}
