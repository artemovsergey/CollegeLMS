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

    public BigBreakResponse? BigBreak { get; set; }

    /// <summary>Если задан — именно этот профиль возвращается как разрешённый на дату.</summary>
    public BellProfileResponse? ResolvedProfile { get; set; }

    public Task<Result<BellScheduleResponse>> GetAsync(CancellationToken ct) =>
        Task.FromResult(Result<BellScheduleResponse>.Ok(new BellScheduleResponse()));

    public Task<Result<BellScheduleResponse>> UpdateAsync(
        UpdateBellScheduleRequest request,
        CancellationToken ct
    ) => Task.FromResult(Result<BellScheduleResponse>.Ok(new BellScheduleResponse()));

    public Task<Dictionary<int, (TimeSpan Start, TimeSpan End)>> GetTimeMapAsync(
        DayOfWeek day,
        CancellationToken ct
    ) => Task.FromResult(TimeMap);

    public Task<Dictionary<int, (TimeSpan Start, TimeSpan End)>> GetTimeMapAsync(
        DateTime date,
        int? substituteDayOfWeek,
        CancellationToken ct
    ) => Task.FromResult(TimeMap);

    public Task<Result<List<BellProfileResponse>>> GetProfilesAsync(CancellationToken ct) =>
        Task.FromResult(Result<List<BellProfileResponse>>.Ok([]));

    public Task<Result<BellProfileResponse>> CreateProfileAsync(
        BellProfileRequest request,
        CancellationToken ct
    ) => Task.FromResult(Result<BellProfileResponse>.Ok(new BellProfileResponse()));

    public Task<Result<BellProfileResponse>> UpdateProfileAsync(
        Guid id,
        BellProfileRequest request,
        CancellationToken ct
    ) => Task.FromResult(Result<BellProfileResponse>.Ok(new BellProfileResponse()));

    public Task<Result> DeleteProfileAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Result.Ok());

    public Task<Result<BellProfileResponse>> GetResolvedAsync(
        DateTime date,
        CancellationToken ct
    ) => GetResolvedAsync(date, null, ct);

    public Task<Result<BellProfileResponse>> GetResolvedAsync(
        DateTime date,
        int? substituteDayOfWeek,
        CancellationToken ct
    )
    {
        if (ResolvedProfile is not null)
            return Task.FromResult(Result<BellProfileResponse>.Ok(ResolvedProfile));

        var profile = new BellProfileResponse
        {
            Id = Guid.NewGuid(),
            Name = "Базовый",
            IsDefault = true,
            Slots = TimeMap
                .OrderBy(kv => kv.Key)
                .Select(kv => new BellSlotResponse
                {
                    Id = Guid.NewGuid(),
                    NumberPair = kv.Key,
                    StartTime = kv.Value.Start,
                    EndTime = kv.Value.End,
                })
                .ToList(),
            BigBreak = BigBreak,
        };

        return Task.FromResult(Result<BellProfileResponse>.Ok(profile));
    }
}
