using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class GroupMapper
{
    public static GroupResponse ToDto(this Group group) =>
        new()
        {
            Id = group.Id,
            Name = group.Name,
            Course = group.Course,
            StudentCount = group.Students.Count,
        };

    public static Group ToEntity(this CreateGroupRequest request) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Course = request.Course,
        };
}
