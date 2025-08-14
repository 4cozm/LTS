using LTS.Base;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using LTS.Services;
using LTS.Utils;
using CommsProto;
using LTS.Models;
using LTS.Data.Repository;
using LTS.Models.Base;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace LTS.Pages.UsingPrepaid
{
    public class IndexModel(ILogger Logger, PrepaidCardRepository cardRepo, SendProtoMessage sender) : BasePageModel
    {
        [BindProperty]
        public string? PhoneNumber { get; set; }

        [BindProperty]
        public string? VerificationCode { get; set; }

        public bool IsCodeSent => HttpContext.Session.GetString("VerificationCodeSent") == "true";

        public List<PrepaidCardViewModel> Cards { get; set; } = new();

        public Employee? CurrentEmployee { get; set; }


        // 발송 요청
        public IActionResult OnPostSendCode()
        {
            PhoneNumber = Regex.Replace(PhoneNumber ?? "", @"\D", ""); // 숫자만 남기기
            ClearVerificationSession();
            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                ModelState.AddModelError(nameof(PhoneNumber), "휴대폰 번호를 입력해 주세요.");
                return Page();
            }
            else if (PhoneNumber.Length != 11)
            {
                ModelState.AddModelError(nameof(PhoneNumber), "휴대폰 번호는 11자리여야 합니다.");
                return Page();
            }
            var activeCards = cardRepo.GetPrepaidCardByPhoneNumber(PhoneNumber);

            Cards = activeCards.Select(c => new PrepaidCardViewModel
            {
                PurchaserName = c.PurchaserName ?? "Error",
                FormattedPhoneNumber = Regex.Replace(c.PurchaserContact ?? "", @"(\d{3})(\d{4})(\d{4})", "$1-$2-$3"),
                InitialValue = (int)c.InitialValue,
                RemainingValue = (int)c.RemainingValue,
                ExpiresAt = c.ExpiresAt?.ToLocalTime(),
                Code = c.Code
            }).ToList();
            if (Cards.Count == 0)
            {
                ModelState.AddModelError(nameof(VerificationCode), "해당 번호로 등록된 선불권이 존재하지 않습니다."); //플로팅 에러를 적용을 위함
            }
            HttpContext.Session.SetString("PhoneNumber", PhoneNumber!);


            return Page();
        }
        public async Task<IActionResult> OnPostRequestVerification(int? UseAmount, string PrepaidCardCode)
        {
            try
            {
                PhoneNumber = HttpContext.Session.GetString("PhoneNumber");
                var card = cardRepo.GetPrepaidCardByCode(PrepaidCardCode);
                if (card == null)
                {
                    ModelState.AddModelError("", "해당 선불권을 찾을 수 없습니다.");
                    return Page();
                }
                if (PhoneNumber is null)
                {
                    ModelState.AddModelError("", "전화번호 정보가 유실되었습니다.");
                    return Page();
                }

                // 1. 사용 가능 수량 확인
                if (UseAmount is null)
                {
                    ModelState.AddModelError("", "사용값이 전달되지 않았습니다.");
                    return Page();
                }
                if (UseAmount == 0)
                {
                    ModelState.AddModelError("", "0개는 사용할 수 없습니다");
                    return Page();
                }
                else if (UseAmount > card.RemainingValue)
                {
                    ModelState.AddModelError("", $"사용 가능 수량({card.RemainingValue})을 초과했습니다.");
                    return Page();
                }

                // 2. 만료일 확인 (null이면 무제한)
                if (card.ExpiresAt is DateTime expiry && expiry < DateTime.UtcNow)
                {
                    ModelState.AddModelError("", "이 선불권은 이미 만료되었습니다.");
                    return Page();
                }

                // 3. 활성 상태 확인
                if (!card.IsActive)
                {
                    ModelState.AddModelError("", "이 선불권은 비활성화된 상태입니다.");
                    return Page();
                }
                string VerifyCode = GenerateCodeUtils.GenerateVerificationCode();
                Console.WriteLine("인증번호" + VerifyCode);

                HttpContext.Session.SetString("VerificationCode", VerifyCode);
                HttpContext.Session.SetString("VerificationCodeExpires", DateTime.UtcNow.AddMinutes(3).ToString());
                HttpContext.Session.SetString("VerificationCodeSent", "true");
                HttpContext.Session.SetInt32("UseAmount", UseAmount.Value);
                HttpContext.Session.SetString("PrepaidCardCode", PrepaidCardCode);

                var VerificationCodeEnvelope = new Envelope
                {
                    KakaoAlert = new SendKakaoAlertNotification
                    {
                        TemplateTitle = "인증번호",
                        Receiver = PhoneNumber,
                        Variables =
                    {
                        { "인증번호", VerifyCode },
                    }
                    }
                };
                await sender.SendMessageAsync(VerificationCodeEnvelope);

                TempData["Message"] = $"{UseAmount.Value}개의 선불권을 사용합니다. 고객의 휴대전화로 인증번호가 발송되었습니다.";
                return RedirectToPage();
            }
            catch (Exception e)
            {
                Console.WriteLine("선불권 사용처리중 오류" + e);
                ModelState.AddModelError("", "선불권 사용처리중 오류가 발생했습니다.");
                return Page();
            }
        }

        // 인증번호 확인
        public async Task<IActionResult> OnPostVerifyAsync(string action)
        {
            // 0) reset 액션
            if (action == "reset")
            {
                ClearVerificationSession();
                return RedirectToPage();
            }

            // 1) 세션 값 로드
            var storedCode = HttpContext.Session.GetString("VerificationCode");
            var expiresRaw = HttpContext.Session.GetString("VerificationCodeExpires");
            var PhoneNumber = HttpContext.Session.GetString("PhoneNumber");
            var UseAmount = HttpContext.Session.GetInt32("UseAmount");
            var PrepaidCardCode = HttpContext.Session.GetString("PrepaidCardCode");

            // 2) 공통 검증(여기서 리턴하는 건 예외 아님: 사용자 과실/만료 케이스)
            if (PhoneNumber == null)
                return NoticeService.RedirectWithNotice(HttpContext, "인증번호 전송기록에 오류가 발생했습니다. 처음부터 다시 시도해 주세요", "/UsingPrepaid");

            if (!DateTime.TryParse(expiresRaw, out var expires) || DateTime.UtcNow > expires)
            {
                ClearVerificationSession();
                return NoticeService.RedirectWithNotice(HttpContext, "인증번호가 만료되었습니다.", "/UsingPrepaid");
            }

            if (string.IsNullOrWhiteSpace(VerificationCode))
            {
                ModelState.AddModelError(nameof(VerificationCode), "인증번호를 입력해 주세요.");
                return Page();
            }

            if (VerificationCode != storedCode)
            {
                ModelState.AddModelError(nameof(VerificationCode), "인증번호가 일치하지 않습니다.");
                return Page();
            }

            if (UseAmount is null || UseAmount <= 0)
            {
                ModelState.AddModelError("", "수량 정보가 없습니다.");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(PrepaidCardCode))
            {
                ModelState.AddModelError("", "선불권 정보가 유실 되었습니다.");
                return Page();
            }

            // 3) 직원정보 확인
            if (HttpContext.Items.TryGetValue("Employee", out var employeeObj))
                CurrentEmployee = employeeObj as Employee;

            if (CurrentEmployee?.Store is null)
                return NoticeService.RedirectWithNotice(HttpContext, "직원 정보 또는 매장 코드가 없습니다.", "/UsingPrepaid");

            // 4) 핵심 로직: 사용 처리 + 알림 발송
            try
            {
                // 4-1) 사용 처리
                var usage = new PrepaidCardUsageLogs
                {
                    ActionType = "USE",
                    ChangeAmount = UseAmount.Value,
                    HandlerName = CurrentEmployee.Name,
                    Reason = "선불권 사용",
                    StoreCode = CurrentEmployee.Store,
                    LoggedAt = DateTime.UtcNow
                };

                // 도메인/레포 호출에서 발생 가능한 예외를 아래 catch에서 구분
                var updatedCard = cardRepo.UsePrepaidCardByCode(PrepaidCardCode, UseAmount.Value, usage);

                // 4-2) 알림 발송 (발송 실패는 사용 성공과 분리)
                string? alertFailMessage = null;
                try
                {
                    var PrepaidEnvelope = new Envelope
                    {
                        KakaoAlert = new SendKakaoAlertNotification
                        {
                            TemplateTitle = "선불권 사용 안내",
                            Receiver = PhoneNumber,
                            Variables =
                    {
                        {"고객명", updatedCard.PurchaserName},
                        {"사용개수", UseAmount.ToString()},
                        {"잔여개수", updatedCard.RemainingValue.ToString("0")},
                        {"처리일시", PrintCurrentDate.PrintDate()},
                        {"매장전화번호", StoreService.GetStorePhoneNumber(CurrentEmployee.Store)}
                    }
                        }
                    };
                    await sender.SendMessageAsync(PrepaidEnvelope);
                }
                catch (Exception ex) // 네트워크/API 실패만 잡아 사용자에게만 안내, 사용 자체는 성공
                {
                    Logger.LogWarning(ex, "카카오 알림 발송 실패. CardCode={CardCode}, Phone={Phone}", PrepaidCardCode, PhoneNumber);
                    alertFailMessage = " (알림 발송은 실패했습니다)";
                }

                // 4-3) 성공 종료: 세션 정리 후 성공 안내
                ClearVerificationSession();
                return NoticeService.RedirectWithNotice(
                    HttpContext,
                    $"{UseAmount.Value}개의 선불권 사용이 완료되었습니다.{alertFailMessage}",
                    "/Home"
                );
            }
            // ====== 예외 핸들링 영역 ======
            catch (ArgumentOutOfRangeException ex) // 잘못된 수량 등 파라미터 이슈
            {
                Logger.LogWarning(ex, "잘못된 요청 파라미터. Code={CardCode}, Use={Use}", PrepaidCardCode, UseAmount);
                return NoticeService.RedirectWithNotice(HttpContext, "요청한 수량이 올바르지 않습니다.", "/UsingPrepaid");
            }
            catch (InvalidOperationException ex) // 카드 미존재/비활성 등 상태 이슈
            {
                Logger.LogWarning(ex, "도메인 상태 오류. Code={CardCode}", PrepaidCardCode);
                return NoticeService.RedirectWithNotice(HttpContext, ex.Message, "/UsingPrepaid");
            }
            catch (DbException ex) // DB 계층 예외 (또는 MySqlException)
            {
                Logger.LogError(ex, "DB 오류. Code={CardCode}", PrepaidCardCode);
                return NoticeService.RedirectWithNotice(HttpContext, "시스템 오류가 발생했습니다. 잠시 후 다시 시도해 주세요.", "/UsingPrepaid");
            }
            catch (TimeoutException ex) // 레포/DB 타임아웃
            {
                Logger.LogError(ex, "처리 타임아웃. Code={CardCode}", PrepaidCardCode);
                return NoticeService.RedirectWithNotice(HttpContext, "처리가 지연되고 있습니다. 잠시 후 다시 시도해 주세요.", "/UsingPrepaid");
            }
            catch (Exception ex) // 마지막 방어선
            {
                Logger.LogError(ex, "알 수 없는 오류. Code={CardCode}", PrepaidCardCode);
                return NoticeService.RedirectWithNotice(HttpContext, "알 수 없는 오류가 발생했습니다.", "/UsingPrepaid");
            }
        }

        private void ClearVerificationSession()
        {
            var session = HttpContext.Session;
            session.Remove("VerificationCode");
            session.Remove("VerificationCodeExpires");
            session.Remove("VerificationCodeSent");
            session.Remove("PhoneNumber");
            session.Remove("UseAmount");
            session.Remove("PrepaidCardCode");
        }
    }

}
