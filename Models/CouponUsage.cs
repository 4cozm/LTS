namespace LTS.Models;

public class CouponUsage
{
    public int Id { get; set; }
    public int UserCouponId { get; set; }
    public string ActionType { get; set; } = "USE"; // ENUM('USE', 'CANCEL', 'RESTORE', 'TEST')
    public DateTime UsedAt { get; set; }
    public string? Note { get; set; }
}