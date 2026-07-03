using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class StudentMapper
{
    public static StudentResponse ToDto(this Student student) =>
        new()
        {
            Id = student.Id,
            FullName = student.User.FullName,
            Email = student.User.Email,
            GroupName = student.Group.Name,
            RecordBookNumber = student.RecordBookNumber,
        };
}
