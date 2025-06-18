public class ConsentData
{
    public string PhoneNumber { get; set; } = "";
    public string? Name { get; set; }
    public string? TermVersion { get; set; }
    public DateTime? SentAt { get; set; }      // 인증 발송 시각
    public DateTime? AgreedAt { get; set; }    // 동의 시각
    public string? StoreCode { get; set; }

    public bool IsAgreed => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(TermVersion);
    public string FormattedPhoneNumber =>
        System.Text.RegularExpressions.Regex.Replace(PhoneNumber, @"(\d{3})(\d{3,4})(\d{4})", "$1-$2-$3");
}