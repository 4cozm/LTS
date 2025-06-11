using LTS.Base;
using System.ComponentModel.DataAnnotations;
using LTS.Models;
using Microsoft.AspNetCore.Mvc;
using LTS.Services;
using LTS.Data.Repository;
using CommsProto;

namespace LTS.Pages.ChangePassword
{
    public class IndexModel(EmployeeRepository employeeRepository, SendProtoMessage sender) : BasePageModel
    {
        private readonly EmployeeRepository _employeeRepository = employeeRepository;
        private readonly SendProtoMessage _sender = sender;

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
        private string? Token { get; set; }
        public bool IsLogin { get; private set; }


        public IActionResult OnGet()
        {
            Token = Request.Cookies["LTS-Session"];
            if (string.IsNullOrEmpty(Token))
            {
                return NoticeService.RedirectWithNotice(HttpContext, "로그인이 필요합니다.", "/Index");
            }

            (IsLogin, CurrentEmployee) = LoginService.TryGetValidEmployeeFromToken(Token);
            if (!IsLogin || CurrentEmployee == null)
            {
                return NoticeService.RedirectWithNotice(HttpContext, "세션이 만료되었거나 유효하지 않습니다.", "/Index");
            }

            PhoneNumber = CurrentEmployee.PhoneNumber;
            if (PhoneNumber == null)
            {
                return NoticeService.RedirectWithNotice(HttpContext, "휴대폰 번호를 불러오는데 실패했습니다.", "/Index");
            }
            HttpContext.Session.SetString("PhoneNumber", PhoneNumber);
            SyncSessionToViewModel();
            return Page();
        }

        public async Task<IActionResult> OnPostSendCode()
        {
            // 인증번호 생성
            string code = GenerateVerificationCode();
            var session = HttpContext.Session;
            PhoneNumber = session.GetString("PhoneNumber");
            Console.WriteLine("휴대폰 번호 잘 저장되어 있나?" + PhoneNumber);

            // 세션에 저장
            HttpContext.Session.SetString("VerificationCode", code);
            HttpContext.Session.SetString("VerificationCodeExpires", DateTime.UtcNow.AddMinutes(3).ToString());
            HttpContext.Session.SetString("VerificationCodeSent", "true");

            // 문자 발송 (서버에서 외부 API 호출)
            var VerificationCodeEnvelope = new Envelope
            {
                KakaoAlert = new SendKakaoAlertNotification
                {
                    TemplateTitle = "고객 인증 요청",
                    Receiver = PhoneNumber,
                    Variables =
                    {
                        { "인증번호", code ?? "" },
                    }
                }
            };
            await _sender.SendMessageAsync(VerificationCodeEnvelope);

            return NoticeService.RedirectWithNotice(HttpContext, "인증번호를 발송했습니다. 인증번호는 3분간 유효합니다.", "/ChangePassword");
        }

        public IActionResult OnPostVerify()
        {
            var session = HttpContext.Session;
            var storedCode = session.GetString("VerificationCode");
            var expirationStr = session.GetString("VerificationCodeExpires");

            if (!DateTime.TryParse(expirationStr, out var expiration) || DateTime.UtcNow > expiration)
            {
                ModelState.AddModelError(string.Empty, "인증번호가 만료되었습니다.");
                ClearVerificationSession();
                SyncSessionToViewModel();
                return Page();
            }

            if (string.IsNullOrEmpty(storedCode))
            {
                ModelState.AddModelError(string.Empty, "인증번호가 전송되지 않았습니다.");
                SyncSessionToViewModel();
                return Page();
            }

            if (VerificationCode == storedCode)
            {
                session.SetString("IsVerified", "true");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "인증번호가 일치하지 않습니다.");
            }

            SyncSessionToViewModel();
            return Page();
        }


        public IActionResult OnPostChangePassword()
        {
            var isVerified = HttpContext.Session.GetString("IsVerified") == "true";
            if (!isVerified)
            {
                return NoticeService.RedirectWithNotice(HttpContext, "인증되지 않은 요청입니다.", "/ChangePassword");
            }

            if (!ModelState.IsValid)
            {
                SyncSessionToViewModel();
                return Page();
            }

            // 비밀번호 변경 로직
            //1.직원 정보 한번 더 확인
            if (CurrentEmployee == null)
            {
                ModelState.AddModelError(string.Empty, "직원 정보가 존재하지 않습니다.");
                return Page();
            }
            //2.기존 비밀번호와 일치하는지 확인
            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(CurrentPassword, CurrentEmployee.Password);
            if (!isPasswordCorrect)
            {
                ModelState.AddModelError(string.Empty, "비밀번호가 일치하지 않습니다.");
                return Page();
            }
            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            _employeeRepository.UpdatePassword(CurrentEmployee.Id, newPasswordHash);

            ClearVerificationSession();

            //TODO 비밀번호 변경 문자 발송
            return NoticeService.RedirectWithNotice(HttpContext, "비밀번호가 성공적으로 변경되었습니다.", "/Home");
        }


        //헬퍼 함수
        private static string GenerateVerificationCode(int length = 6)
        {
            const string chars = "0123456789";
            var random = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
        private void SyncSessionToViewModel()
        {
            IsVerified = HttpContext.Session.GetString("IsVerified") == "true";
            IsCodeSent = HttpContext.Session.GetString("VerificationCodeSent") == "true";
        }
        private void ClearVerificationSession()
        {
            var session = HttpContext.Session;
            session.Remove("IsVerified");
            session.Remove("VerificationCode");
            session.Remove("VerificationCodeExpires");
            session.Remove("VerificationCodeSent");
            session.Remove("PhoneNumber");
        }

    }

}
