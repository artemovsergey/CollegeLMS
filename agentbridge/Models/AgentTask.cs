namespace AgentBridge.Models;

public enum AgentTaskStatus
{
    Pending,
    Running,
    WaitingPermission,
    WaitingForReply,
    Completed,
    Failed,
    Cancelled
}

public class PendingPermission
{
    public string PermissionId { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string ToolName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public class AgentTask
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Prompt { get; init; } = string.Empty;
    public string? Result { get; set; }
    public AgentTaskStatus Status { get; set; } = AgentTaskStatus.Pending;
    public string? ErrorMessage { get; set; }
    public string? OpenCodeSessionId { get; set; }
    public long ChatId { get; init; }
    public string Messenger { get; init; } = "telegram";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public PendingPermission? CurrentPermission { get; set; }
}
