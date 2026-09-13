namespace CollegeLMS.API.Entities;

public class CorrectionConfirmation : Entity
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public int HistoryCount { get; set; }
}