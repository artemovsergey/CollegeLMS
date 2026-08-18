using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class CourseDocumentMapper
{
    public static CourseDocumentResponse ToDto(this CourseDocument doc) =>
        new()
        {
            Id = doc.Id,
            CourseId = doc.CourseId,
            FileName = doc.FileName,
            ContentType = doc.ContentType,
            SizeBytes = doc.SizeBytes,
            CreatedAt = doc.CreatedAt,
        };
}
