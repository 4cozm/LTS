public class PrepaidCard
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Type { get; set; } = ""; // Can be "AMOUNT" or "COUNT"
    public decimal InitialValue { get; set; }
    public decimal RemainingValue { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? PurchaserName { get; set; }
    public string? PurchaserContact { get; set; }
    public string? Notes { get; set; }
    public string? StoreCode { get; set; }
}
