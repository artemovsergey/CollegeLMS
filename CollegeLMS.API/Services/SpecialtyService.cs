using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class SpecialtyService(AppDbContext db) : ISpecialtyService
{
    public async Task<Result<List<SpecialtyResponse>>> GetAllAsync(string? search, CancellationToken ct)
    {
        var query = db.Specialties.AsNoTracking().Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s =>
                s.Name.Contains(search) || s.Code.Contains(search));

        var specialties = await query.OrderBy(s => s.Code).ToListAsync(ct);
        return Result<List<SpecialtyResponse>>.Ok(specialties.Select(s => s.ToDto()).ToList());
    }

    public async Task<Result<SpecialtyResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var specialty = await db.Specialties.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (specialty is null)
            return Result<SpecialtyResponse>.Fail("Специальность не найдена", 404);
        return Result<SpecialtyResponse>.Ok(specialty.ToDto());
    }

    public async Task<Result<SpecialtyResponse>> CreateAsync(CreateSpecialtyRequest request, CancellationToken ct)
    {
        var exists = await db.Specialties.AnyAsync(s => s.Code == request.Code && !s.IsDeleted, ct);
        if (exists)
            return Result<SpecialtyResponse>.Fail("Специальность с таким кодом уже существует", 409);

        var specialty = new Specialty
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Department = request.Department,
        };
        db.Specialties.Add(specialty);
        await db.SaveChangesAsync(ct);
        return Result<SpecialtyResponse>.Ok(specialty.ToDto());
    }

    public async Task<Result<SpecialtyResponse>> UpdateAsync(Guid id, UpdateSpecialtyRequest request, CancellationToken ct)
    {
        var specialty = await db.Specialties.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (specialty is null)
            return Result<SpecialtyResponse>.Fail("Специальность не найдена", 404);

        var codeExists = await db.Specialties.AnyAsync(s => s.Code == request.Code && s.Id != id && !s.IsDeleted, ct);
        if (codeExists)
            return Result<SpecialtyResponse>.Fail("Специальность с таким кодом уже существует", 409);

        specialty.Code = request.Code;
        specialty.Name = request.Name;
        specialty.Description = request.Description;
        specialty.Department = request.Department;
        specialty.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<SpecialtyResponse>.Ok(specialty.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var specialty = await db.Specialties.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (specialty is null)
            return Result.Fail("Специальность не найдена", 404);

        specialty.IsDeleted = true;
        specialty.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
