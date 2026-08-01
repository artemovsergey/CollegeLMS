namespace CollegeLMS.API.Dtos;

public class UserProfileResponse
{
    public UserResponse User { get; set; } = null!;
    public List<UserCourseItem> Courses { get; set; } = new();
    public List<UserNewsItem> News { get; set; } = new();
}

public class UserCourseItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class UserNewsItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
}
