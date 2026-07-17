using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class TeacherMapper
{
    public static TeacherResponse ToDto(this Teacher teacher) =>
        new()
        {
            Id = teacher.Id,
            FullName = teacher.User.FullName,
            Email = teacher.User.Email,
            CyclicalCommission = teacher.CyclicalCommission,
            Position = teacher.Position,
        };
}
