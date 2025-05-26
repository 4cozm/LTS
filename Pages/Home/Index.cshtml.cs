using System.ComponentModel.DataAnnotations;
using LTS.Base;
using Microsoft.AspNetCore.Mvc;

namespace LTS.Pages.Home
{
    public class IndexModel : BasePageModel
    {
        [BindProperty]
        public string PhoneNumber { get; set; } = "에러:핸드폰 번호를 찾을 수 없습니다";

        [BindProperty]
        [Required(ErrorMessage = "인증번호를 입력해주세요.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "인증번호는 6자리 숫자여야 합니다.")]
        public string VerificationCode { get; set; } = "null";

        [BindProperty]
        [Required(ErrorMessage = "현재 비밀번호를 입력해주세요.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "새로운 비밀번호를 입력해주세요.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "새로운 비밀번호는 6자리 숫자여야 합니다.")]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "비밀번호를 다시 입력해주세요.")]
        [Compare("NewPassword", ErrorMessage = "비밀번호가 일치하지 않습니다.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public void OnGet()
        {
            // 초기 처리
        }
        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            // 비밀번호 변경 로직 등 처리
            return RedirectToPage("/Success");
        }

    }
}