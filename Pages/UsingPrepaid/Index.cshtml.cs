using LTS.Base;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using LTS.Services;
using LTS.Utils;
using CommsProto;
using LTS.Models;
using LTS.Data.Repository;

namespace LTS.Pages.UsingPrepaid
{
    public class IndexModel(SendProtoMessage sender) : BasePageModel
    {
        [BindProperty]
        public string? PhoneNumber { get; set; }

        [BindProperty]
        public string? VerificationCode { get; set; }

        public bool IsCodeSent => HttpContext.Session.GetString("VerificationCodeSent") == "true";

        public List<PrepaidCardViewModel> Cards { get; set; } = new();

        // 발송 요청
        public async Task<IActionResult> OnPostSendCodeAsync()
        {
            PhoneNumber = Regex.Replace(PhoneNumber ?? "", @"\D", ""); // 숫자만 남기기

            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                ModelState.AddModelError(nameof(PhoneNumber), "휴대폰 번호를 입력해 주세요.");
                return Page();
            }
            else if (PhoneNumber.Length != 11)
            {
                ModelState.AddModelError(nameof(PhoneNumber), "휴대폰 번호는 11자리여야 합니다.");
                return Page();
            }


            var cardRepo = new PrepaidCardRepository();
            var activeCards = cardRepo.GetPrepaidCardByPhoneNumber(PhoneNumber);

            Cards = activeCards.Select(c => new PrepaidCardViewModel
            {
                PurchaserName = c.PurchaserName ?? "정보 없음",
                FormattedPhoneNumber = Regex.Replace(c.PurchaserContact ?? "", @"(\d{3})(\d{4})(\d{4})", "$1-$2-$3"),
                InitialValue = (int)c.InitialValue,
                RemainingValue = (int)c.RemainingValue,
                ExpiresAt = c.ExpiresAt?.ToLocalTime()
            }).ToList();


            return Page();
        }


        // 인증번호 확인
        public IActionResult OnPostVerify(string action)
        {
            var storedCode = HttpContext.Session.GetString("VerificationCode");
            var expiresRaw = HttpContext.Session.GetString("VerificationCodeExpires");
            var PhoneNumber = HttpContext.Session.GetString("PhoneNumber");

            if (action == "reset")
            {
                ClearVerificationSession();
                return RedirectToPage();
            }


            if (!DateTime.TryParse(expiresRaw, out var expires) || DateTime.UtcNow > expires)
            {
                ClearVerificationSession();
                return NoticeService.RedirectWithNotice(HttpContext, "인증번호가 만료되었습니다.", "/PurchasePrepaid");
            }
            if (VerificationCode == null)
            {
                ModelState.AddModelError(nameof(VerificationCode), "인증번호를 입력해 주세요.");
                return Page();
            }

            if (VerificationCode != storedCode)
            {
                ModelState.AddModelError(nameof(VerificationCode), "인증번호가 일치하지 않습니다.");
                return Page();
            }

            TempData["Message"] = "인증이 완료되었습니다.";
            ClearVerificationSession();
            // TODO: 인증 이후 처리 로직 (ex. 상품 선택 단계로 넘어가기)
            return RedirectToPage("SelectProduct"); // 또는 Page()로 머무르기
        }

        public void OnGet()
        {

        }
        private void ClearVerificationSession()
        {
            var session = HttpContext.Session;
            session.Remove("VerificationCode");
            session.Remove("VerificationCodeExpires");
            session.Remove("VerificationCodeSent");
            session.Remove("PhoneNumber");
        }
    }

}
