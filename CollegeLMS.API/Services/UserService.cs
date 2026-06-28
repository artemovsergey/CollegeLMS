using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Response;
using CollegeLMS.API.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class UserService(AppDbContext db) : IUserService
{
    public async Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken ct)
    {
        var users = await db.Set<User>().AsNoTracking().OrderBy(x => x.FullName).ToListAsync(ct);

        return Result<List<UserResponse>>.Ok(users.Select(x => x.ToDto()).ToList());
    }
}
