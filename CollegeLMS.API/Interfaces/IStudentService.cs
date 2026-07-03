using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IStudentService
{
    Task<Result<List<StudentResponse>>> GetAllAsync(Guid? groupId = null, CancellationToken ct = default);
    Task<Result<StudentResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<StudentResponse>> CreateAsync(CreateStudentRequest request, CancellationToken ct = default);
    Task<Result<StudentResponse>> UpdateAsync(Guid id, UpdateStudentRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
