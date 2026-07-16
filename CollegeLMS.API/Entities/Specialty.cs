using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class Specialty : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
