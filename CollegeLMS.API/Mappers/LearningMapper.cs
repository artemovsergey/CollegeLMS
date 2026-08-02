using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class SpecialtyMapper
{
    public static SpecialtyResponse ToDto(this Specialty specialty) =>
        new()
        {
            Id = specialty.Id,
            Code = specialty.Code,
            Name = specialty.Name,
            Description = specialty.Description,
            Department = specialty.Department,
        };
}

public static class TransferMapper
{
    public static TransferRecordResponse ToDto(this TransferRecord record) =>
        new()
        {
            Id = record.Id,
            StudentId = record.StudentId,
            FromGroupId = record.FromGroupId,
            FromGroupName = string.Empty,
            ToGroupId = record.ToGroupId,
            ToGroupName = string.Empty,
            Reason = record.Reason,
            CreatedAt = record.CreatedAt,
        };
}
