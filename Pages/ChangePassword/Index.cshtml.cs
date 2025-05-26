using LTS.Base;
using System.ComponentModel.DataAnnotations;
using LTS.Models;
using Microsoft.AspNetCore.Mvc;
using LTS.Services;

namespace LTS.Pages.ChangePassword
{
    public class IndexModel : BasePageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? PhoneNumber { get; set; }

        [BindProperty]
        public string? VerificationCode { get; set; }


        [BindProperty]
        [Required(ErrorMessage = "현재 비밀번호를 입력해주세요")]
        public string? CurrentPassword { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "새로운 비밀번호를 입력해주세요")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "새로운 비밀번호는 6자리 숫자여야 합니다.")]
        public string? NewPassword { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "비밀번호를 다시 입력해주세요")]
        [Compare("NewPassword", ErrorMessage = "비밀번호가 일치하지 않습니다.")]
        public string? ConfirmPassword { get; set; }

        public Employee? CurrentEmployee { get; private set; }

        public bool IsCodeSent { get; private set; } = false;
        public bool IsVerified { get; private set; } = false;

        public void OnGet()
        {
            CurrentEmployee = HttpContext.Items["Employee"] as Employee;
            if (CurrentEmployee == null)
            {
                ModelState.AddModelError(string.Empty, "로그인 된 직원 정보를 찾을 수 없습니다.");
                return;
            }
            PhoneNumber = CurrentEmployee.PhoneNumber;
            IsCodeSent = HttpContext.Session.GetString("VerificationCodeSent") == "true";
            IsVerified = HttpContext.Session.GetString("IsVerified") == "true";
        }


        public IActionResult OnPostVerify()
        {
            var session = HttpContext.Session;
            var storedCode = session.GetString("VerificationCode");
            var expirationStr = session.GetString("VerificationCodeExpires");

            // 1. 인증번호 유효시간 검사
            if (!DateTime.TryParse(expirationStr, out var expiration) || DateTime.UtcNow > expiration)
            {
                ModelState.AddModelError(string.Empty, "인증번호가 만료되었습니다.");
                session.Remove("VerificationCode");
                session.Remove("VerificationCodeExpires");
                session.Remove("VerificationCodeSent");
                return Page();
            }

            // 2. 인증번호 존재 여부 검사
            if (string.IsNullOrEmpty(storedCode))
            {
                ModelState.AddModelError(string.Empty, "인증번호가 전송되지 않았습니다.");
                return Page();
            }

            // 3. 사용자가 입력한 인증번호와 비교
            if (VerificationCode == storedCode)
            {
                session.SetString("IsVerified", "true");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "인증번호가 일치하지 않습니다.");
            }

            return Page();
        }


        public IActionResult OnPostChangePassword()
        {
            if (!IsVerified)
            {
                return NoticeService.RedirectWithNotice(HttpContext, "인증되지 않은 요청입니다.", "/ChangePassword");
            }
            if (!ModelState.IsValid)
            {
                return Page();
            }
            // 비밀번호 변경 로직 수행
            CurrentEmployee = HttpContext.Items["Employee"] as Employee;

            HttpContext.Session.Remove("IsVerified");
            HttpContext.Session.Remove("VerificationCode");
            HttpContext.Session.Remove("VerificationCodeExpires");
            HttpContext.Session.Remove("VerificationCodeSent");

            return NoticeService.RedirectWithNotice(HttpContext, "비밀번호가 성공적으로 변경되었습니다.", "/Home");
        }

        public IActionResult OnPostSendCode()
        {
            // 인증번호 생성
            string code = GenerateVerificationCode();
            // 세션에 저장
            HttpContext.Session.SetString("VerificationCode", code);
            HttpContext.Session.SetString("VerificationCodeExpires", DateTime.UtcNow.AddMinutes(3).ToString());
            HttpContext.Session.SetString("VerificationCodeSent", "true");

            // 문자 발송 (서버에서 외부 API 호출)


            return NoticeService.RedirectWithNotice(HttpContext, "인증번호를 발송했습니다. 인증번호는 3분간 유효합니다.", "/ChangePassword");
        }
        private static string GenerateVerificationCode(int length = 6)
        {
            const string chars = "0123456789";
            var random = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
    }
}
