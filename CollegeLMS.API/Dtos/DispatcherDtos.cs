namespace CollegeLMS.API.Dtos;

public class DispatcherLoginRequest
{
    public string Password { get; set; } = string.Empty;
}

public class DispatcherLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
