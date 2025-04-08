namespace LTS.Models;

public class PrepaidCardUsage
{
    public int Id { get; set; }
    public int PrepaidCardId { get; set; }
    public string ActionType { get; set; } = "USE"; // ENUM('USE', 'RESTORE', 'ADJUST', 'TEST')
    public decimal ChangeAmount { get; set; }
    public string? UsageNote { get; set; }
    public DateTime UsedAt { get; set; }
}
