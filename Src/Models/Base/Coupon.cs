namespace LTS.Models;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public string DiscountType { get; set; } = ""; // "PERCENT" or "FIXED"
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int ValidDays { get; set; }
    public bool IsActive { get; set; }
}
