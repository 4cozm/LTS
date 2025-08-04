namespace LTS.Models.Base;


public class PrepaidCardUsageState
{
    public int Id { get; set; }
    public int PrepaidCardId { get; set; }
    public decimal UsedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string StoreCode { get; set; } = string.Empty;
}
