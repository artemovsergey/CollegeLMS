using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class PracticeMapper
{
    public static PracticeResponse ToDto(this Practice entity)
    {
        var teachers = entity
            .Teachers.OrderBy(t => t.Teacher?.User?.FullName ?? string.Empty)
            .Select(t => new PracticeTeacherResponse
            {
                Id = t.TeacherId,
                Name = t.Teacher?.User?.FullName ?? string.Empty,
            })
            .ToList();

        return new()
        {
            Id = entity.Id,
            Kind = entity.Kind,
            Name = entity.Name,
            GroupId = entity.GroupId,
            GroupName = entity.Group?.Name ?? string.Empty,
            TeacherIds = teachers.Select(t => t.Id).ToList(),
            Teachers = teachers,
            DateFrom = entity.DateFrom,
            DateTo = entity.DateTo,
            Days = entity
                .Days.OrderBy(d => d.Date)
                .Select(d => new PracticeDayDto
                {
                    Date = d.Date,
                    PairNumbers = d.PairNumbers.OrderBy(n => n).ToList(),
                })
                .ToList(),
            Note = entity.Note,
            Room = entity.Room,
            Subgroup = entity.Subgroup,
        };
    }
}
