using LTS.Base;
using Microsoft.AspNetCore.Mvc;

namespace LTS.Pages.PurchasePrepaid
{
    public class IndexModel : BasePageModel
    {
        // 1) 폼 바인딩 속성
        [BindProperty]
        public string PhoneNumber { get; set; } = "";

        [BindProperty]
        public string VerificationCode { get; set; } = "";

        // (선택) 화면에서 사용할 메시지
        public string? StatusMessage { get; set; }

        private const string SessionKeyCode = "VerificationCode";
        private const string SessionKeySentTime = "CodeSentTime";

    }
}