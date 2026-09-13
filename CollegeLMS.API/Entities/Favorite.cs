using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class Favorite : Entity
{
    public Guid UserId { get; set; }
    public FavoriteTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
}
