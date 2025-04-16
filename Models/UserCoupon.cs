namespace LTS.Models;

public class UserCoupon
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CouponId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public bool IsUsed { get; set; }
    public bool DirtyFlag { get; set; }
}
