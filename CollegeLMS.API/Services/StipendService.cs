using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class StipendService(AppDbContext db) : IStipendService
{
    public async Task<Result<StipendListDetailResponse>> GenerateAsync(
        Guid semesterId,
        CancellationToken ct
    )
    {
        var semester = await db.Semesters.FindAsync([semesterId], ct);
        if (semester is null)
            return Result<StipendListDetailResponse>.Fail("Семестр не найден", 404);

        var students = await db
            .Students.AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Group)
            .Where(s => s.Group != null)
            .ToListAsync(ct);

        var eligibleStudents = new List<(Student student, double avgScore)>();

        foreach (var student in students)
        {
            var submissions = await db
                .AssignmentSubmissions.AsNoTracking()
                .Where(s => s.StudentId == student.Id && s.Score.HasValue)
                .ToListAsync(ct);

            if (submissions.Count == 0)
                continue;

            var avgScore = submissions.Average(s => s.Score!.Value);

            var hasDebts = submissions.Any(s => s.Score < 60);

            if (!hasDebts && avgScore >= 4.0)
                eligibleStudents.Add((student, avgScore));
        }

        var list = new StipendList
        {
            Id = Guid.NewGuid(),
            SemesterId = semesterId,
            Name = $"Стипендиальный список — {semester.Name}",
        };
        db.StipendLists.Add(list);
        await db.SaveChangesAsync(ct);

        foreach (var (student, avgScore) in eligibleStudents)
        {
            var amount = avgScore >= 4.5 ? 3000m : 2000m;

            db.StipendListItems.Add(
                new StipendListItem
                {
                    Id = Guid.NewGuid(),
                    StipendListId = list.Id,
                    StudentId = student.Id,
                    Amount = amount,
                    AverageScore = avgScore,
                }
            );
        }

        await db.SaveChangesAsync(ct);

        return new StipendListDetailResponse
        {
            Id = list.Id,
            Name = list.Name,
            SemesterName = semester.Name,
            Students = eligibleStudents
                .Select(s => new StipendStudentResponse
                {
                    StudentId = s.student.Id,
                    StudentName = s.student.User?.FullName ?? string.Empty,
                    GroupName = s.student.Group?.Name ?? string.Empty,
                    AverageScore = s.avgScore,
                    Amount = s.avgScore >= 4.5 ? 3000m : 2000m,
                })
                .ToList(),
        };
    }

    public async Task<Result<List<StipendListResponse>>> GetAllAsync(CancellationToken ct)
    {
        var lists = await db
            .StipendLists.AsNoTracking()
            .Include(l => l.Semester)
            .Include(l => l.Items)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        return Result<List<StipendListResponse>>.Ok(lists.Select(l => l.ToListDto()).ToList());
    }

    public async Task<Result<StipendListDetailResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var list = await db
            .StipendLists.AsNoTracking()
            .Include(l => l.Semester)
            .Include(l => l.Items)
                .ThenInclude(i => i.Student)
                    .ThenInclude(s => s.User)
            .Include(l => l.Items)
                .ThenInclude(i => i.Student)
                    .ThenInclude(s => s.Group)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (list is null)
            return Result<StipendListDetailResponse>.Fail("Ведомость не найдена", 404);

        return Result<StipendListDetailResponse>.Ok(
            new StipendListDetailResponse
            {
                Id = list.Id,
                Name = list.Name,
                SemesterName = list.Semester?.Name ?? string.Empty,
                Students = list
                    .Items.Select(i => new StipendStudentResponse
                    {
                        StudentId = i.StudentId,
                        StudentName = i.Student?.User?.FullName ?? string.Empty,
                        GroupName = i.Student?.Group?.Name ?? string.Empty,
                        AverageScore = i.AverageScore,
                        Amount = i.Amount,
                    })
                    .ToList(),
            }
        );
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var list = await db.StipendLists.FindAsync([id], ct);
        if (list is null)
            return Result.Fail("Ведомость не найдена", 404);

        db.StipendLists.Remove(list);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
