using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using LTS.Validators;
using LTS.Data.Repository;
using LTS.Models;
using LTS.Base;
using LTS.Services;
using CommsProto;


namespace LTS.Pages.Register
{
    public class IndexModel : BasePageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "이름을 입력해주세요.")]
        [StringLength(5, MinimumLength = 2, ErrorMessage = "이름은 2~5글자여야 합니다.")]
        [RegularExpression(@"^[가-힣]+$", ErrorMessage = "이름은 한글만 포함해야 합니다.")]
        public string? Name { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "이니셜을 입력해주세요.")]
        [StringLength(5, MinimumLength = 2, ErrorMessage = "이니셜은 2~5글자여야 합니다.")]
        [RegularExpression(@"^[A-Z0-9]{2,5}$", ErrorMessage = "이니셜은 영어 대문자,숫자만 포함해야 합니다.")]
        public string? Initial { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "전화번호를 입력해주세요.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "전화번호는 숫자 11자리만 가능합니다.")]
        public string? PhoneNumber { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "날짜가 입력되지 않았습니다")]
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
        private readonly SendProtoMessage _sender;

        public IndexModel(EmployeeRepository employeeRepository, SendProtoMessage sender)
        {
            _employeeRepository = employeeRepository;
            _sender = sender;
        }

        public async Task<IActionResult> OnPost()
        {
            try
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

                var token = Request.Cookies["LTS-Session"];
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("[ALERT] 미들웨어 검증 무시하고 접근중");
                    ModelState.AddModelError(string.Empty, "잘못된 방법으로 접근하셨습니다.");
                    return Page();
                }

                var (isValid, employee) = LoginService.TryGetValidEmployeeFromToken(token);
                if (!isValid || employee == null)
                {
                    NoticeService.RedirectWithNotice(HttpContext, "세션이 만료되었거나 유효하지 않습니다", "/Index");
                    return new EmptyResult();
                }

                Console.WriteLine($"[LOG] {RoleName} 등록 요청중 : {employee.Name}");
                if (employee.RoleName != "Manager" && employee.RoleName != "Owner")
                {
                    Console.WriteLine($"[ALERT] 스태프 등록 권한 없음 : {employee.Name}");
                    ModelState.AddModelError(string.Empty, "권한이 부족합니다.");
                    return Page();
                }

                if (RoleName == "Manager" && employee.RoleName != "Owner")
                {
                    Console.WriteLine($"[ALERT] 매니저 등록 권한 없음 : {employee.Name}");
                    ModelState.AddModelError(string.Empty, "권한이 부족합니다.");
                    return Page();
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(PhoneNumber);

                var newEmployee = new Employee
                {
                    Name = Name!,
                    PhoneNumber = PhoneNumber!,
                    Initials = Initial!,
                    Password = hashedPassword,
                    Store = StoreName!,
                    RoleName = RoleName!,
                    WorkStartDate = EffectiveDate ?? DateTime.Today,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByMember = employee.Name
                };

                var created = _employeeRepository.CreateEmployee(newEmployee);

                if (created == null)
                {
                    ModelState.AddModelError(string.Empty, "알 수 없는 이유로 직원 등록에 실패했습니다.");
                    return Page();
                }
                var authEnvelope = new Envelope
                {
                    KakaoAlert = new SendKakaoAlertNotification
                    {
                        TemplateTitle = "직원 등록 안내",
                        Receiver = PhoneNumber,
                        Variables =
                    {
                        { "직원명", Name ?? "" },
                        { "이니셜", Initial ?? "" },
                        { "전화번호", PhoneNumber ?? "" }
                    }
                    }
                };
                await _sender.SendMessageAsync(authEnvelope);

                Console.WriteLine($"[LOG] 직원 등록 완료 : {newEmployee.Initials}, {newEmployee.Store}, {newEmployee.RoleName}, {newEmployee.CreatedByMember}");
                return RedirectToPage("/Result/Success");

            }
            catch (Exception e)
            {
                Console.WriteLine("Register 페이지에서 예외 발생", e.Message);
                return RedirectToPage("/Result/Failed");
            }

        }
    }
}
