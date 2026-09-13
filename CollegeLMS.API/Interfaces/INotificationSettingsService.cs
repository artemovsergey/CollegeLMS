using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface INotificationSettingsService
{
    Task<Result<NotificationSettingsResponse>> GetAsync(Guid userId, CancellationToken ct);
    Task<Result<NotificationSettingsResponse>> UpdateAsync(
        Guid userId,
        UpdateNotificationSettingsRequest request,
        CancellationToken ct
    );
}