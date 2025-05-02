using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using LTS.Validators;

namespace LTS.Pages.Register
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        [StringLength(5, MinimumLength = 2, ErrorMessage = "이니셜은 2~5글자여야 합니다.")]
        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "이니셜은 영문자만 포함해야 합니다.")]
        public string? Initial { get; set; }

        [BindProperty]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "전화번호는 숫자 11자리만 가능합니다.")]
        public string? PhoneNumber { get; set; }

        [BindProperty]
        [DataType(DataType.Date, ErrorMessage = "유효한 날짜를 입력하세요.")]
        public DateTime? EffectiveDate { get; set; }

        [BindProperty]
        [ValidName]
        public string? StoreName { get; set; }

        [BindProperty]
        [ValidRoleName]
        public string? RoleName { get; set; }


        public IActionResult OnPost()
        {
            if (EffectiveDate > DateTime.Today)
            {
                ModelState.AddModelError("EffectiveDate", "미래는 선택할 수 없습니다 - SKY NET");
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }
            //1. 토큰을 검사해서 이름,직책 가져옴
            //2. 권한 있는지 조건문으로 확인
            //3. DB에 저장
            //4. 문자발송(예정)


            return RedirectToPage("Success"); // 성공 페이지로 리디렉션 (예정)
        }
    }


}