using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;

namespace CollegeLMS.API.Mappers;

public static class ImportJobMapper
{
    public static ImportProgressDto ToDto(this ImportJob job) =>
        new()
        {
            ImportId = job.Id.ToString(),
            Status = job.Status,
            Total = job.Total,
            Processed = job.Processed,
            Errors = job.ErrorCount,
            ErrorMessages = job.ErrorMessages ?? [],
            Result = new ImportResult(
                job.CategoriesCreated,
                job.PostsImported,
                job.PostsSkipped,
                job.ErrorMessages ?? []
            ),
            CreatedAt = job.CreatedAt,
        };
}
