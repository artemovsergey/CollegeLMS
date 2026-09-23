using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

/// <summary>Импорт и экспорт графика учебной практики (УП) в формате DOCX.</summary>
public interface IPracticeGraphService
{
    /// <summary>Разбор DOCX-графика: строки-подгруппы, дни и номера пар, ошибки.</summary>
    Task<Result<PracticeGraphPreviewResponse>> PreviewImportAsync(
        Stream docx,
        CancellationToken ct
    );

    /// <summary>Подтверждение импорта: создание УП-практик группы по строкам графика.</summary>
    Task<Result<PracticeGraphConfirmResponse>> ConfirmImportAsync(
        PracticeGraphConfirmRequest request,
        CancellationToken ct
    );

    /// <summary>Формирование DOCX-графика по всем УП-практикам группы.</summary>
    Task<Result<DocumentDownloadResult>> ExportAsync(Guid groupId, CancellationToken ct);
}
