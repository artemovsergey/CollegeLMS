using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;

namespace CollegeLMS.Tests.Fixtures;

/// <summary>
/// Простой стаб справочника звонков: по умолчанию карта времени пуста,
/// поэтому сервисы используют сохранённые в записи StartTime/EndTime.
/// </summary>
public sealed class BellScheduleServiceStub : IBellScheduleService
{
    public Dictionary<int, (TimeSpan Start, TimeSpan End)> TimeMap { get; set; } = new();

    public Task<Result<BellScheduleResponse>> GetAsync(CancellationToken ct) =>
        Task.FromResult(Result<BellScheduleResponse>.Ok(new BellScheduleResponse()));

    public Task<Result<BellScheduleResponse>> UpdateAsync(
        UpdateBellScheduleRequest request,
        CancellationToken ct
    ) => Task.FromResult(Result<BellScheduleResponse>.Ok(new BellScheduleResponse()));

    public Task<Dictionary<int, (TimeSpan Start, TimeSpan End)>> GetTimeMapAsync(
        CancellationToken ct
    ) => Task.FromResult(TimeMap);
}
