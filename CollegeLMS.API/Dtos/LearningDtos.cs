namespace CollegeLMS.API.Dtos;

public class CreateSemesterRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Type { get; set; } = "Autumn";
    public string AcademicYear { get; set; } = string.Empty;
}

public class UpdateSemesterRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Type { get; set; } = "Autumn";
    public string AcademicYear { get; set; } = string.Empty;
}

public class SemesterResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
}

public class CreateSpecialtyRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

public class UpdateSpecialtyRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

public class SpecialtyResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

public class AssignGroupsRequest
{
    public List<Guid> GroupIds { get; set; } = new();
}

public class CourseGroupResponse
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
}

public class CourseProgressResponse
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int TotalTests { get; set; }
    public int CompletedTests { get; set; }
    public double AverageScore { get; set; }
    public double CompletionPercent { get; set; }
}

public class CreateExamRequest
{
    public string Subject { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public DateTime ExamDate { get; set; }
    public string Type { get; set; } = "Exam";
    public Guid TeacherId { get; set; }
    public Guid SemesterId { get; set; }
}

public class UpdateExamRequest
{
    public string Subject { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public DateTime ExamDate { get; set; }
    public string Type { get; set; } = "Exam";
    public Guid TeacherId { get; set; }
    public Guid SemesterId { get; set; }
    public string Status { get; set; } = "Scheduled";
}

public class ExamResponse
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public DateTime ExamDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public Guid SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class CreateRetakeRequest
{
    public Guid StudentId { get; set; }
    public DateTime RetakeDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class UpdateRetakeStatusRequest
{
    public string Status { get; set; } = "Scheduled";
}

public class RetakeResponse
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateTime RetakeDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class TransferStudentRequest
{
    public Guid NewGroupId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class TransferRecordResponse
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid FromGroupId { get; set; }
    public string FromGroupName { get; set; } = string.Empty;
    public Guid ToGroupId { get; set; }
    public string ToGroupName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class StipendListResponse
{
    public Guid Id { get; set; }
    public Guid SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StipendListDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public List<StipendStudentResponse> Students { get; set; } = new();
}

public class StipendStudentResponse
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public double AverageScore { get; set; }
    public decimal Amount { get; set; }
}

public class StudentImportProgress
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<ImportError> Errors { get; set; } = new();
}
