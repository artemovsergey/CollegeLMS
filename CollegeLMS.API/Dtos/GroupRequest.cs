namespace CollegeLMS.API.Dtos;

public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }
}

public class UpdateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }
}
