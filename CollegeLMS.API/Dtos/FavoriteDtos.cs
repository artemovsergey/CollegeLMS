using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class FavoriteResponse
{
    public Guid Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FavoriteTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string? GroupName { get; set; }
    public string? TeacherName { get; set; }
}

public class AddFavoriteRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FavoriteTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
}
