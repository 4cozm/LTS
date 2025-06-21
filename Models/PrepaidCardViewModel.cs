namespace LTS.Models;

public class PrepaidCardViewModel
{
    public string PurchaserName { get; set; } = string.Empty;
    public string FormattedPhoneNumber { get; set; } = string.Empty;
    public int InitialValue { get; set; }
    public int RemainingValue { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Code { get; set; }
}
