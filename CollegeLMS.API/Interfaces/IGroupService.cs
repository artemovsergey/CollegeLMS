using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IGroupService
{
    Task<Result<List<GroupResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<GroupResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<GroupResponse>> CreateAsync(CreateGroupRequest request, CancellationToken ct = default);
    Task<Result<GroupResponse>> UpdateAsync(Guid id, UpdateGroupRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
