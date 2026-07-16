using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class SemesterMapper
{
    public static SemesterResponse ToDto(this Semester semester) =>
        new()
        {
            Id = semester.Id,
            Name = semester.Name,
            StartDate = semester.StartDate,
            EndDate = semester.EndDate,
            Type = semester.Type.ToString(),
            AcademicYear = semester.AcademicYear,
        };
}

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

public static class ExamMapper
{
    public static ExamResponse ToDto(this Exam exam) =>
        new()
        {
            Id = exam.Id,
            Subject = exam.Subject,
            GroupId = exam.GroupId,
            GroupName = exam.Group?.Name ?? string.Empty,
            ExamDate = exam.ExamDate,
            Type = exam.Type.ToString(),
            TeacherId = exam.TeacherId,
            TeacherName = exam.Teacher?.User?.FullName ?? string.Empty,
            SemesterId = exam.SemesterId,
            SemesterName = exam.Semester?.Name ?? string.Empty,
            Status = exam.Status.ToString(),
        };
}

public static class RetakeMapper
{
    public static RetakeResponse ToDto(this Retake retake) =>
        new()
        {
            Id = retake.Id,
            ExamId = retake.ExamId,
            StudentId = retake.StudentId,
            StudentName = retake.Student?.User?.FullName ?? string.Empty,
            RetakeDate = retake.RetakeDate,
            Reason = retake.Reason,
            Status = retake.Status.ToString(),
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

public static class StipendMapper
{
    public static StipendListResponse ToListDto(this StipendList list) =>
        new()
        {
            Id = list.Id,
            SemesterId = list.SemesterId,
            SemesterName = list.Semester?.Name ?? string.Empty,
            Name = list.Name,
            StudentCount = list.Items?.Count ?? 0,
            TotalAmount = list.Items?.Sum(i => i.Amount) ?? 0,
            CreatedAt = list.CreatedAt,
        };
}
