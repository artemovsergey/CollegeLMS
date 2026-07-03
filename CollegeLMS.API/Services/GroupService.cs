using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class GroupService(AppDbContext db) : IGroupService
{
    public async Task<Result<List<GroupResponse>>> GetAllAsync(CancellationToken ct)
    {
        var groups = await db.Groups
            .AsNoTracking()
            .Include(g => g.Students)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

        return Result<List<GroupResponse>>.Ok(groups.Select(g => g.ToDto()).ToList());
    }

    public async Task<Result<GroupResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var group = await db.Groups
            .AsNoTracking()
            .Include(g => g.Students)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (group is null)
            return Result<GroupResponse>.Fail("Группа не найдена", 404);

        return Result<GroupResponse>.Ok(group.ToDto());
    }

    public async Task<Result<GroupResponse>> CreateAsync(CreateGroupRequest request, CancellationToken ct)
    {
        var exists = await db.Groups.AnyAsync(g => g.Name == request.Name, ct);
        if (exists)
            return Result<GroupResponse>.Fail("Группа с таким названием уже существует", 409);

        var group = request.ToEntity();
        db.Groups.Add(group);
        await db.SaveChangesAsync(ct);

        return Result<GroupResponse>.Ok(group.ToDto());
    }

    public async Task<Result<GroupResponse>> UpdateAsync(Guid id, UpdateGroupRequest request, CancellationToken ct)
    {
        var group = await db.Groups.FindAsync([id], ct);
        if (group is null)
            return Result<GroupResponse>.Fail("Группа не найдена", 404);

        var nameExists = await db.Groups.AnyAsync(g => g.Name == request.Name && g.Id != id, ct);
        if (nameExists)
            return Result<GroupResponse>.Fail("Группа с таким названием уже существует", 409);

        group.Name = request.Name;
        group.Course = request.Course;
        group.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<GroupResponse>.Ok(group.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var group = await db.Groups.FindAsync([id], ct);
        if (group is null)
            return Result.Fail("Группа не найдена", 404);

        db.Groups.Remove(group);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
