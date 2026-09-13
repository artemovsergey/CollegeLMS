using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class FavoritesService(AppDbContext db) : IFavoritesService
{
    public async Task<Result<List<FavoriteResponse>>> GetAllAsync(Guid userId, CancellationToken ct)
    {
        var items = await db
            .Favorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

        var result = new List<FavoriteResponse>();
        foreach (var f in items)
        {
            var dto = f.ToDto();
            await EnrichAsync(dto, ct);
            result.Add(dto);
        }

        return Result<List<FavoriteResponse>>.Ok(result);
    }

    public async Task<Result<FavoriteResponse>> AddAsync(
        Guid userId,
        AddFavoriteRequest request,
        CancellationToken ct
    )
    {
        if (request.TargetType == FavoriteTargetType.Group)
        {
            if (!await db.Groups.AnyAsync(g => g.Id == request.TargetId, ct))
                return Result<FavoriteResponse>.Fail("Группа не найдена", 404);
        }
        else
        {
            if (!await db.Teachers.AnyAsync(t => t.Id == request.TargetId, ct))
                return Result<FavoriteResponse>.Fail("Преподаватель не найден", 404);
        }

        var existing = await db.Favorites.FirstOrDefaultAsync(
            f =>
                f.UserId == userId
                && f.TargetType == request.TargetType
                && f.TargetId == request.TargetId,
            ct
        );
        if (existing is not null)
        {
            var dto = existing.ToDto();
            await EnrichAsync(dto, ct);
            return Result<FavoriteResponse>.Ok(dto);
        }

        var favorite = new Favorite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Favorites.Add(favorite);
        await db.SaveChangesAsync(ct);

        var created = favorite.ToDto();
        await EnrichAsync(created, ct);
        return Result<FavoriteResponse>.Ok(created);
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var favorite = await db.Favorites.FirstOrDefaultAsync(
            f => f.Id == id && f.UserId == userId,
            ct
        );
        if (favorite is null)
            return Result.Fail("Избранное не найдено", 404);

        db.Favorites.Remove(favorite);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private async Task EnrichAsync(FavoriteResponse dto, CancellationToken ct)
    {
        if (dto.TargetType == FavoriteTargetType.Group)
        {
            var group = await db
                .Groups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == dto.TargetId, ct);
            dto.GroupName = group?.Name;
        }
        else
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == dto.TargetId, ct);
            dto.TeacherName = teacher?.User.FullName;
        }
    }
}