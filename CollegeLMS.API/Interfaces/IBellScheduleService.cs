using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IBellScheduleService
{
    /// <summary>Справочник звонков базового (default) профиля.</summary>
    Task<Result<BellScheduleResponse>> GetAsync(CancellationToken ct);

    /// <summary>Заменить пары и большую перемену базового (default) профиля.</summary>
    Task<Result<BellScheduleResponse>> UpdateAsync(
        UpdateBellScheduleRequest request,
        CancellationToken ct
    );

    /// <summary>Время пар по номеру дня недели: профиль дня недели → базовый профиль.</summary>
    Task<Dictionary<int, (TimeSpan Start, TimeSpan End)>> GetTimeMapAsync(
        DayOfWeek day,
        CancellationToken ct
    );

    /// <summary>
    /// Время пар на дату. Приоритет профиля: диапазон даты → подменённый день недели →
    /// день недели даты → базовый профиль.
    /// </summary>
    Task<Dictionary<int, (TimeSpan Start, TimeSpan End)>> GetTimeMapAsync(
        DateTime date,
        int? substituteDayOfWeek,
        CancellationToken ct
    );

    /// <summary>Все профили звонков с парами, большой переменой и диапазонами дат.</summary>
    Task<Result<List<BellProfileResponse>>> GetProfilesAsync(CancellationToken ct);

    /// <summary>Создать профиль звонков (не базовый).</summary>
    Task<Result<BellProfileResponse>> CreateProfileAsync(
        BellProfileRequest request,
        CancellationToken ct
    );

    /// <summary>Изменить профиль звонков по идентификатору.</summary>
    Task<Result<BellProfileResponse>> UpdateProfileAsync(
        Guid id,
        BellProfileRequest request,
        CancellationToken ct
    );

    /// <summary>Удалить профиль звонков. Базовый профиль удалить нельзя.</summary>
    Task<Result> DeleteProfileAsync(Guid id, CancellationToken ct);

    /// <summary>Разрешённый на дату профиль звонков (для UI и live-дашборда).</summary>
    Task<Result<BellProfileResponse>> GetResolvedAsync(DateTime date, CancellationToken ct);

    /// <summary>Разрешённый на дату профиль звонков с учётом подмены дня недели.</summary>
    Task<Result<BellProfileResponse>> GetResolvedAsync(
        DateTime date,
        int? substituteDayOfWeek,
        CancellationToken ct
    );
}
