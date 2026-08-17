using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class MaterialMapper
{
    public static MaterialResponse ToDto(this CourseMaterial material) =>
        new()
        {
            Id = material.Id,
            CourseId = material.CourseId,
            LessonId = material.LessonId,
            FileName = material.FileName,
            FileSize = material.FileSize,
            MimeType = material.MimeType,
            CreatedAt = material.CreatedAt,
        };
}
