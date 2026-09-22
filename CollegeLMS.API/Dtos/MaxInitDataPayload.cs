namespace CollegeLMS.API.Dtos;

public class MaxInitDataPayload
{
    public long MaxUserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? StartParam { get; init; }
}
