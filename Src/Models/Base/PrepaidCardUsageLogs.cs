namespace LTS.Models.Base;

public class PrepaidCardUsageLogs
{
    public int Id { get; set; }
    public int PrepaidCardId { get; set; }
    public string ActionType { get; set; } = "USE";
    public decimal ChangeAmount { get; set; }
    public string? HandlerName { get; set; }
    public string? Reason { get; set; }
    public required string StoreCode { get; set; }
    public DateTime LoggedAt { get; set; }
}