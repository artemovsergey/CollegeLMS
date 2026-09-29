namespace CollegeLMS.API.Entities.Enums;

/// <summary>
/// Статусы пакета корректировки. Отменённых пакетов не существует: применённый
/// пакет можно удалить целиком вместе с его записями журнала.
/// </summary>
public enum CorrectionBatchStatus
{
    Draft,
    Applied,
}
