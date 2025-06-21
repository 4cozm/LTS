using LTS.Base;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using LTS.Services;
using LTS.Utils;
using CommsProto;
using LTS.Models;
using LTS.Data.Repository;
using System.Text.Json;


namespace LTS.Pages.UsingPrepaid
{
    public class IndexModel : BasePageModel
    {
        [BindProperty]
        public string? PhoneNumber { get; set; }

        [BindProperty]
        public string? VerificationCode { get; set; }

        public bool IsCodeSent => HttpContext.Session.GetString("VerificationCodeSent") == "true";

        public List<PrepaidCardViewModel> Cards { get; set; } = new();

        // 발송 요청
        public IActionResult OnPostSendCode()
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
                PurchaserName = c.PurchaserName ?? "Error",
                FormattedPhoneNumber = Regex.Replace(c.PurchaserContact ?? "", @"(\d{3})(\d{4})(\d{4})", "$1-$2-$3"),
                InitialValue = (int)c.InitialValue,
                RemainingValue = (int)c.RemainingValue,
                ExpiresAt = c.ExpiresAt?.ToLocalTime(),
                Code = c.Code
            }).ToList();
            if (Cards.Count == 0)
            {
                ModelState.AddModelError(nameof(VerificationCode), "해당 번호로 등록된 선불권이 존재하지 않습니다.");
            }


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
            if (PhoneNumber == null)
            {
                ModelState.AddModelError(nameof(VerificationCode), "인증번호 전송 내역이 없습니다. 처음부터 다시 시도해 주세요");
                return Page();
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


            ClearVerificationSession();
            return Page(); // 또는 Page()로 머무르기
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
