using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IExamService
{
    Task<Result<List<ExamResponse>>> GetAllAsync(Guid? groupId, Guid? semesterId, string? type, CancellationToken ct = default);
    Task<Result<ExamResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ExamResponse>> CreateAsync(CreateExamRequest request, CancellationToken ct = default);
    Task<Result<ExamResponse>> UpdateAsync(Guid id, UpdateExamRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<RetakeResponse>> CreateRetakeAsync(Guid examId, CreateRetakeRequest request, CancellationToken ct = default);
    Task<Result<List<RetakeResponse>>> GetRetakesAsync(Guid examId, CancellationToken ct = default);
    Task<Result<RetakeResponse>> UpdateRetakeStatusAsync(Guid examId, Guid retakeId, UpdateRetakeStatusRequest request, CancellationToken ct = default);
    Task<Result> DeleteRetakeAsync(Guid examId, Guid retakeId, CancellationToken ct = default);
}
