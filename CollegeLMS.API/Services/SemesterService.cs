using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class SemesterService(AppDbContext db) : ISemesterService
{
    public async Task<Result<List<SemesterResponse>>> GetAllAsync(CancellationToken ct)
    {
        var semesters = await db
            .Semesters.AsNoTracking()
            .OrderByDescending(s => s.StartDate)
            .ToListAsync(ct);
        return Result<List<SemesterResponse>>.Ok(semesters.Select(s => s.ToDto()).ToList());
    }

    public async Task<Result<SemesterResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var semester = await db.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (semester is null)
            return Result<SemesterResponse>.Fail("Семестр не найден", 404);
        return Result<SemesterResponse>.Ok(semester.ToDto());
    }

    public async Task<Result<SemesterResponse>> CreateAsync(
        CreateSemesterRequest request,
        CancellationToken ct
    )
    {
        if (!Enum.TryParse<SemesterType>(request.Type, out var type))
            return Result<SemesterResponse>.Fail("Некорректный тип семестра", 400);

        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Type = type,
            AcademicYear = request.AcademicYear,
        };
        db.Semesters.Add(semester);
        await db.SaveChangesAsync(ct);
        return Result<SemesterResponse>.Ok(semester.ToDto());
    }

    public async Task<Result<SemesterResponse>> UpdateAsync(
        Guid id,
        UpdateSemesterRequest request,
        CancellationToken ct
    )
    {
        var semester = await db.Semesters.FindAsync([id], ct);
        if (semester is null)
            return Result<SemesterResponse>.Fail("Семестр не найден", 404);

        if (!Enum.TryParse<SemesterType>(request.Type, out var type))
            return Result<SemesterResponse>.Fail("Некорректный тип семестра", 400);

        semester.Name = request.Name;
        semester.StartDate = request.StartDate;
        semester.EndDate = request.EndDate;
        semester.Type = type;
        semester.AcademicYear = request.AcademicYear;
        semester.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<SemesterResponse>.Ok(semester.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var semester = await db.Semesters.FindAsync([id], ct);
        if (semester is null)
            return Result.Fail("Семестр не найден", 404);

        db.Semesters.Remove(semester);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
