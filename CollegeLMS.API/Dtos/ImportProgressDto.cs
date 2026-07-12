using CollegeLMS.API.Interfaces;

namespace CollegeLMS.API.Dtos;

public class ImportProgressDto
{
    public string ImportId { get; set; } = string.Empty;
    public string Status { get; set; } = "running";
    public int Total { get; set; }
    public int Processed { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorMessages { get; set; } = [];
    public ImportResult? Result { get; set; }
}
