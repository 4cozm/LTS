using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using LTS.Validators;
using LTS.Data.Repository;
using LTS.Models;

namespace LTS.Pages.Register
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        [StringLength(5, MinimumLength = 2, ErrorMessage = "이니셜은 2~5글자여야 합니다.")]
        [RegularExpression(@"^[A-Z]+$", ErrorMessage = "이니셜은 영어 대문자만 포함해야 합니다.")]
        public string? Initial { get; set; }

        [BindProperty]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "전화번호는 숫자 11자리만 가능합니다.")]
        public string? PhoneNumber { get; set; }

        [BindProperty]
        [DataType(DataType.Date, ErrorMessage = "유효한 날짜를 입력하세요.")]
        public DateTime? EffectiveDate { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "매장을 선택해주세요.")]
        [ValidName]
        public string? StoreName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "직책을 선택해주세요.")]
        [ValidRoleName]
        public string? RoleName { get; set; }

        private readonly EmployeeRepository _employeeRepository;

        public IndexModel(EmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public IActionResult OnPost()
        {
            if (EffectiveDate > DateTime.Today)
            {
                ModelState.AddModelError("EffectiveDate", "미래는 선택할 수 없습니다");
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // 1. 토큰을 검사해서 이름, 직책 가져옴
                // 2. 권한 있는지 조건문으로 확인
                // 3. DB에 저장
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(PhoneNumber); // 비밀번호 해싱
                var employee = new Employee
                {
                    Initials = Initial,
                    Password = hashedPassword,
                    Store = StoreName,
                    RoleName = RoleName,
                    WorkStartDate = EffectiveDate ?? DateTime.Today, // 값을 선택하지 않으면 오늘 날짜로 설정
                    CreatedAt = DateTime.UtcNow,
                    CreatedByMember = "SystemAdmin"
                };

                var createEmployee = _employeeRepository.CreateEmployee(employee);

                if (createEmployee == null)
                {
                    // DB에 저장 실패 시
                    ModelState.AddModelError(string.Empty, "직원 등록에 실패했습니다. 해당 에러 내용은 관리자에게 자동으로 전달됩니다.");
                    return Page();
                }

                Console.WriteLine($"직원 등록 완료 : {employee.Initials}, {employee.Store}, {employee.RoleName}"); //추후 로깅

                // 4. 문자 발송 (예정)
                // 문자 발송 관련 코드 추가 예정

                return RedirectToPage("/"); // 성공 페이지로 리디렉션(예정)
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "직원 등록 중 오류가 발생했습니다. 해당 에러 내용은 관리자에게 자동으로 전달됩니다.");
                return Page();
            }
        }

    }
}