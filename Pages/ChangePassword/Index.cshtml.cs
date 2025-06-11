using LTS.Base;
using System.ComponentModel.DataAnnotations;
using LTS.Models;
using Microsoft.AspNetCore.Mvc;
using LTS.Services;
using LTS.Data.Repository;
using CommsProto;
using LTS.Utils;

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
        public string? ConfirmPassword { get; set; }

        public Employee? CurrentEmployee { get; private set; }

        public bool IsCodeSent { get; private set; } = false;
        public bool IsVerified { get; private set; } = false;
        private string? Token { get; set; }
        public bool IsLogin { get; private set; }


        public IActionResult OnGet()
        {
            try
            {
                if (!TryLoadCurrentEmployee())
                {
                    return NoticeService.RedirectWithNotice(HttpContext, "로그인 정보를 찾을 수 없습니다", "/Index");
                }

                PhoneNumber = CurrentEmployee!.PhoneNumber;
                if (PhoneNumber == null)
                {
                    return NoticeService.RedirectWithNotice(HttpContext, "휴대폰 번호를 불러오는데 실패했습니다.", "/Index");
                }
                HttpContext.Session.SetString("PhoneNumber", PhoneNumber);
                SyncSessionToViewModel();
                return Page();
            }
            catch (Exception e)
            {
                ClearVerificationSession();
                Console.Error.WriteLine("알 수 없는 에러 발생", e.Message);
                return NoticeService.RedirectWithNotice(HttpContext, "알 수 없는 오류가 발생했습니다. 관리자에게 문의 하세요", "/Index");
            }
        }

        public async Task<IActionResult> OnPostSendCode()
        {
            try
            {
                // 인증번호 생성
                string code = GenerateVerificationCode();
                var session = HttpContext.Session;
                PhoneNumber = session.GetString("PhoneNumber");

                // 문자 발송 (서버에서 외부 API 호출)
                var VerificationCodeEnvelope = new Envelope
                {
                    KakaoAlert = new SendKakaoAlertNotification
                    {
                        TemplateTitle = "인증번호",
                        Receiver = PhoneNumber,
                        Variables =
                    {
                        { "인증번호", code ?? "" },
                    }
                    }
                };
                await _sender.SendMessageAsync(VerificationCodeEnvelope);
                // 문자 발송에 에러 없었다면 세션에 저장  

                HttpContext.Session.SetString("VerificationCode", code!);
                HttpContext.Session.SetString("VerificationCodeExpires", DateTime.UtcNow.AddMinutes(3).ToString());
                HttpContext.Session.SetString("VerificationCodeSent", "true");

                return NoticeService.RedirectWithNotice(HttpContext, "인증번호를 발송했습니다. 인증번호는 3분간 유효합니다.", "/ChangePassword");
            }
            catch (Exception e)
            {
                ClearVerificationSession();
                Console.Error.WriteLine("알 수 없는 에러 발생", e.Message);
                return NoticeService.RedirectWithNotice(HttpContext, "알 수 없는 오류가 발생했습니다. 관리자에게 문의 하세요", "/Index");
            }
        }

        public IActionResult OnPostVerify()
        {
            try
            {
                if (!TryLoadCurrentEmployee())
                {
                    return NoticeService.RedirectWithNotice(HttpContext, "로그인 정보를 찾을 수 없습니다.", "/Index");
                }

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
                    SyncSessionToViewModel();
                    return NoticeService.RedirectWithNotice(HttpContext, "인증번호가 일치합니다. 비밀번호를 변경해주세요.", "/ChangePassword");
                }

                return NoticeService.RedirectWithNotice(HttpContext, "인증번호가 일치하지 않습니다", "/ChangePassword");

            }
            catch (Exception e)
            {
                ClearVerificationSession();
                Console.Error.WriteLine("알 수 없는 에러 발생", e.Message);
                return NoticeService.RedirectWithNotice(HttpContext, "알 수 없는 오류가 발생했습니다. 관리자에게 문의 하세요", "/Index");
            }
        }



        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            try
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
                if (!TryLoadCurrentEmployee())
                {
                    return NoticeService.RedirectWithNotice(HttpContext, "로그인 정보를 찾을 수 없습니다.", "/Index");
                }
                //2.기존 비밀번호와 일치하는지 확인
                bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(CurrentPassword, CurrentEmployee!.Password);
                if (!isPasswordCorrect)
                {
                    return NoticeService.RedirectWithNotice(HttpContext, "기존 비밀번호가 올바르지 않습니다", "/ChangePassword");
                }
                if (NewPassword != ConfirmPassword)
                {
                    return NoticeService.RedirectWithNotice(HttpContext, "새로 입력하신 두 비밀번호가 서로 일치하지 않습니다", "/ChangePassword");
                }
                string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                _employeeRepository.UpdatePassword(CurrentEmployee.Id, newPasswordHash);

                var PasswordChangedEnvelope = new Envelope
                {
                    KakaoAlert = new SendKakaoAlertNotification
                    {
                        TemplateTitle = "비밀번호 변경 안내",
                        Receiver = CurrentEmployee.PhoneNumber,
                        Variables =
                    {
                        {"직원명",CurrentEmployee.Name},
                        {"변경일시",PrintCurrentDate.PrintDate()},
                        {"변경IP",PrintCurrentIp.GetLocalIPAddress()},
                        {"관리자연락처","010-8820-5076"}
                    }
                    }
                };
                await _sender.SendMessageAsync(PasswordChangedEnvelope);
                SessionStore.RemoveSession(Token!);
                ClearVerificationSession();
                return NoticeService.RedirectWithNotice(HttpContext, "비밀번호가 성공적으로 변경되었습니다. 다시 로그인 해 주세요", "/Index");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("알 수 없는 에러 발생", e.Message);
                ClearVerificationSession();
                return NoticeService.RedirectWithNotice(HttpContext, "알 수 없는 오류가 발생했습니다. 관리자에게 문의 하세요", "/Index");
            }
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
        private bool TryLoadCurrentEmployee()
        {
            Token = Request.Cookies["LTS-Session"];
            if (string.IsNullOrEmpty(Token)) return false;

            (IsLogin, CurrentEmployee) = LoginService.TryGetValidEmployeeFromToken(Token);
            return IsLogin && CurrentEmployee != null;
        }

    }

}
