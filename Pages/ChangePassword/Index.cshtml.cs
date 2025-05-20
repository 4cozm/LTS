using LTS.Base;
using System.ComponentModel.DataAnnotations;
using LTS.Models;
using Microsoft.AspNetCore.Mvc;

namespace LTS.Pages.ChangePassword
{
    public class IndexModel : BasePageModel
    {
        [BindProperty]
        public string? PhoneNumber { get; private set; } 

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

        public bool IsVerified { get; private set; } = false;

        public void OnGet()
        {
            CurrentEmployee = HttpContext.Items["Employee"] as Employee;
            PhoneNumber = CurrentEmployee.PhoneNumber;
        }

    public IActionResult OnPostVerify()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        PhoneNumber = employee?.PhoneNumber;

        if (VerificationCode == "123456") // 예시: 인증번호 매칭 로직
        {
            IsVerified = true;
        }
        else
        {
            ModelState.AddModelError(string.Empty, "인증번호가 일치하지 않습니다.");
        }

        return Page();
    }

    public IActionResult OnPostChangePassword()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        PhoneNumber = employee?.PhoneNumber;
        IsVerified = true; // 이미 인증됐다고 간주

        if (!ModelState.IsValid) return Page();

        // 비밀번호 변경 로직 수행

        return RedirectToPage("/Success");
    }
    }
}
