namespace CollegeLMS.API.Dtos;

public class ScheduleImportResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<ImportError> Errors { get; set; } = [];
}

public class ImportError
{
    public int Row { get; set; }
    public string Message { get; set; } = string.Empty;
}
