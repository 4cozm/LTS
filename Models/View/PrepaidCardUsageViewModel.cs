public class PrepaidCardUsageViewModel
{
    public int UsageId { get; set; }
    public string? PrepaidCardCode { get; set; }
    public int PrepaidCardId { get; set; }
    public string? ActionType { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? UsageNote { get; set; }
    public DateTime UsedAt { get; set; }

    public string PurchaserName { get; set; } = "";
    public string PurchaserContact { get; set; } = "";
}
