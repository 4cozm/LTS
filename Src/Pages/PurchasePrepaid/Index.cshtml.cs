using Microsoft.AspNetCore.Mvc;
using LTS.Utils;
using LTS.Services;
using System.Text.RegularExpressions;
using System.Text.Json;
using LTS.Models;
using CommsProto;
using LTS.Base;


namespace LTS.Pages.PurchasePrepaid
{
    public class IndexModel(RedisService redis, SendProtoMessage sender) : BasePageModel
    {

        [BindProperty]
        public string? PhoneNumber { get; set; }

        [BindProperty]
        public string? VerificationCode { get; set; }
        public Employee? CurrentEmployee { get; private set; }

        public bool IsCodeSent => HttpContext.Session.GetString("VerificationCodeSent") == "true";

        public async Task<IActionResult> OnPostSendCodeAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                ModelState.AddModelError(nameof(PhoneNumber), "전화번호를 입력해주세요.");
                return Page();
            }
            var cleanedPhone = Regex.Replace(PhoneNumber ?? "", @"\D", ""); //문자 제거
            if (cleanedPhone.Length != 11)
            {
                ModelState.AddModelError(nameof(PhoneNumber), "전화번호는 숫자 11자리여야 합니다.");
                return Page();
            }

            string code = GenerateCodeUtils.GenerateVerificationCode();
            HttpContext.Session.SetString("PhoneNumber", PhoneNumber!);
            HttpContext.Session.SetString("VerificationCode", code);
            HttpContext.Session.SetString("VerificationCodeExpires", DateTime.UtcNow.AddMinutes(3).ToString());
            HttpContext.Session.SetString("VerificationCodeSent", "true");
            Console.WriteLine("인증코드" + code);

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
            await sender.SendMessageAsync(VerificationCodeEnvelope);

            TempData["Message"] = "인증번호가 발송되었습니다. 3분 내에 입력해주세요.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostVerifyAsync(string action)
        {
            var storedCode = HttpContext.Session.GetString("VerificationCode");
            var expiresRaw = HttpContext.Session.GetString("VerificationCodeExpires");
            var PhoneNumber = HttpContext.Session.GetString("PhoneNumber");

            if (action == "reset")
            {
                ClearVerificationSession();
                return RedirectToPage();
            }


            if (!DateTime.TryParse(expiresRaw, out var expires) || DateTime.UtcNow > expires)
            {
                ClearVerificationSession();
                return NoticeService.RedirectWithNotice(HttpContext, "인증번호가 만료되었습니다.", "/PurchasePrepaid");
            }
            if (VerificationCode == null)
            {
                ModelState.AddModelError(nameof(VerificationCode), "인증번호를 입력해 주세요.");
                return Page();
            }

            if (VerificationCode != storedCode)
            {
                ModelState.AddModelError(nameof(VerificationCode), "인증번호가 일치하지 않습니다.");
                return Page();
            }
            //약관 동의서 발송 코드
            if (HttpContext.Items.TryGetValue("Employee", out var employeeObj))
            {
                CurrentEmployee = employeeObj as Employee;
            }

            var authEnvelope = new Envelope
            {
                KakaoAlert = new SendKakaoAlertNotification
                {
                    TemplateTitle = "약관 동의서 발송",
                    Receiver = PhoneNumber,
                    Variables =
                    {
                        { "매장명", "구매 매장:"+StoreService.GetStoreDisplayName(CurrentEmployee!.Store!) ?? "" },
                        { "일시",  PrintCurrentDate.PrintDate() ?? "" },
                    }
                }
            };
            await sender.SendMessageAsync(authEnvelope);

            var db = redis.GetDatabase();
            var key = $"consent:{PhoneNumber}";
            var data = new ConsentData
            {
                PhoneNumber = PhoneNumber!,
                SentAt = DateTime.UtcNow,
                StoreCode = CurrentEmployee.Store
            };

            await db.StringSetAsync(key, JsonSerializer.Serialize(data), TimeSpan.FromHours(24));

            ClearVerificationSession();
            return NoticeService.RedirectWithNotice(HttpContext, "인증이 완료되었습니다. 고객의 휴대폰으로 동의서가 발송 되었습니다.", "/Home");
        }


        private void ClearVerificationSession()
        {
            var session = HttpContext.Session;
            session.Remove("VerificationCode");
            session.Remove("VerificationCodeExpires");
            session.Remove("VerificationCodeSent");
            session.Remove("PhoneNumber");
        }

    }

}
