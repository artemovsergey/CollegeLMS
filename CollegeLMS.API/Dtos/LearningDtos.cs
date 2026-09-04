namespace CollegeLMS.API.Dtos;

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
    public int TotalTests { get; set; }
    public int CompletedTests { get; set; }
    public double CompletionPercent { get; set; }
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

public class StudentImportProgress
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<ImportError> Errors { get; set; } = new();
}

public class ImportError
{
    public int Row { get; set; }
    public string Message { get; set; } = string.Empty;
}
