using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace LTS.Pages.PurchasePrepaid
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "전화번호를 입력해주세요.")]
        public string? PhoneNumber { get; set; }

        [BindProperty]
        public string? VerificationCode { get; set; }

        public bool IsCodeSent => HttpContext.Session.GetString("VerificationCodeSent") == "true";
        public bool IsVerified => HttpContext.Session.GetString("IsVerified") == "true";

        public IActionResult OnPostSendCode()
        {
            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                ModelState.AddModelError(nameof(PhoneNumber), "전화번호를 입력해주세요.");
                return Page();
            }

            string code = GenerateVerificationCode();
            HttpContext.Session.SetString("VerificationCode", code);
            HttpContext.Session.SetString("VerificationCodeExpires", DateTime.UtcNow.AddMinutes(3).ToString());
            HttpContext.Session.SetString("VerificationCodeSent", "true");

            // 실제 인증번호 발송 API 호출 부분 생략

            TempData["Message"] = "인증번호가 발송되었습니다. 3분 내에 입력해주세요.";
            return RedirectToPage();
        }

        public IActionResult OnPostVerify()
        {
            var storedCode = HttpContext.Session.GetString("VerificationCode");
            var expiresRaw = HttpContext.Session.GetString("VerificationCodeExpires");

            if (string.IsNullOrEmpty(storedCode) || !DateTime.TryParse(expiresRaw, out var expires) || DateTime.UtcNow > expires)
            {
                ModelState.AddModelError(string.Empty, "인증번호가 만료되었거나 전송되지 않았습니다.");
                return Page();
            }

            if (VerificationCode != storedCode)
            {
                ModelState.AddModelError(nameof(VerificationCode), "인증번호가 일치하지 않습니다.");
                return Page();
            }

            HttpContext.Session.SetString("IsVerified", "true");
            TempData["Message"] = "인증이 완료되었습니다.";
            return RedirectToPage();
        }

        private static string GenerateVerificationCode(int length = 6)
        {
            const string digits = "0123456789";
            var rand = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => digits[rand.Next(digits.Length)]).ToArray());
        }
    }
}
