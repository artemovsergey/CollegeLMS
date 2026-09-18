using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IBellScheduleService
{
    Task<Result<BellScheduleResponse>> GetAsync(CancellationToken ct);

    Task<Result<BellScheduleResponse>> UpdateAsync(
        UpdateBellScheduleRequest request,
        CancellationToken ct
    );

    /// <summary>Время пар по номерам из справочника (пустой словарь, если справочник не задан).</summary>
    Task<Dictionary<int, (TimeSpan Start, TimeSpan End)>> GetTimeMapAsync(CancellationToken ct);
}
