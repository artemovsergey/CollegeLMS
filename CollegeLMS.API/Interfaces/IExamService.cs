using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IExamService
{
    Task<Result<List<ExamResponse>>> GetAllAsync(
        Guid? groupId,
        Guid? semesterId,
        string? type,
        CancellationToken ct = default
    );
    Task<Result<ExamResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ExamResponse>> CreateAsync(
        CreateExamRequest request,
        CancellationToken ct = default
    );
    Task<Result<ExamResponse>> UpdateAsync(
        Guid id,
        UpdateExamRequest request,
        CancellationToken ct = default
    );
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
