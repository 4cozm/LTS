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
        [Required(ErrorMessage = "인증 번호를 입력해주세요")]
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
            if (VerificationCode == "123456") // 예시: 인증번호 매칭 로직
            {
                HttpContext.Session.SetString("IsVerified", "true");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "인증번호가 일치하지 않습니다.");
            }

            return Page();
        }

        public IActionResult OnPostChangePassword()
        {
            IsVerified = HttpContext.Session.GetString("IsVerified") == "true";
            if (!IsVerified)
            {
                return NoticeService.RedirectWithNotice(HttpContext, "인증되지 않은 요청입니다.", "/ChangePassword");
            }
            if (!ModelState.IsValid)
            {
                return Page();
            }
            // 비밀번호 변경 로직 수행

            HttpContext.Session.Remove("IsVerified");
            HttpContext.Session.Remove("VerificationCode");
            HttpContext.Session.Remove("VerificationCodeExpires");
            HttpContext.Session.Remove("VerificationCodeSent");

            return NoticeService.RedirectWithNotice(HttpContext, "비밀번호가 성공적으로 변경되었습니다.", "/Home");
        }

        public IActionResult OnPostSendCode()
        {
            // 인증번호 생성

            // 세션에 저장
            HttpContext.Session.SetString("VerificationCode", code);
            HttpContext.Session.SetString("VerificationCodeExpires", DateTime.UtcNow.AddMinutes(3).ToString());
            HttpContext.Session.SetString("VerificationCodeSent", "true");

            // 문자 발송 (서버에서 외부 API 호출)


            return NoticeService.RedirectWithNotice(HttpContext, "인증번호를 발송했습니다. 인증번호는 3분간 유효합니다.", "/ChangePassword");
        }
    }
}
