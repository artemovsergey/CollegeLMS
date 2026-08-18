using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class CourseAccessService(AppDbContext db) : ICourseAccessService
{
    public async Task<bool> CanManageCourseAsync(
        Guid courseId,
        Guid teacherId,
        CancellationToken ct
    )
    {
        var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
            return false;
        return await CanManageCourseAsync(course, teacherId, ct);
    }

    public async Task<bool> CanManageCourseAsync(
        Course course,
        Guid teacherId,
        CancellationToken ct
    )
    {
        if (course.TeacherId == teacherId)
            return true;

        return await db
            .CourseAuthors.AsNoTracking()
            .AnyAsync(a => a.CourseId == course.Id && a.TeacherId == teacherId, ct);
    }

    public async Task<List<Guid>> GetManagedCourseIdsAsync(Guid teacherId, CancellationToken ct)
    {
        var ownedIds = db
            .Courses.AsNoTracking()
            .Where(c => c.TeacherId == teacherId)
            .Select(c => c.Id);
        var coAuthorIds = db
            .CourseAuthors.AsNoTracking()
            .Where(a => a.TeacherId == teacherId)
            .Select(a => a.CourseId);

        return await ownedIds.Concat(coAuthorIds).Distinct().ToListAsync(ct);
    }
}
