using LTS.Base;
using LTS.Models;
using LTS.Data.Repository;
using Microsoft.AspNetCore.Mvc;
using LTS.Services;
using CommsProto;
using LTS.Utils;


namespace LTS.Pages.RefundPrepaid
{
    public class IndexModel(PrepaidCardRepository cardRepo, SendProtoMessage sender) : BasePageModel
    {
        [BindProperty]
        public string? PhoneNumber { get; set; }

        public Employee? CurrentEmployee { get; set; }
        public List<PrepaidCardUsageViewModel> UsageList { get; set; } = new();

        public List<PrepaidCard> PrepaidCardList { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                CurrentEmployee = HttpContext.Items["Employee"] as Employee;
                if (CurrentEmployee == null)
                {
                    ViewData["Error"] = "직원 정보가 없습니다. 다시 로그인해주세요.(서버 오류)";
                    return;
                }

                var storeCode = CurrentEmployee.Store;
                UsageList = await cardRepo.GetPrepaidUsageWithUserInfoAsync(storeCode);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[오류] 선불권 상세조회 중 예외 발생: {e.Message}\n{e.StackTrace}");
                TempData["Error"] = "선불권 상세조회 중 오류가 발생했습니다";
                return;
            }
        }

        public async Task<IActionResult> OnPostCancelPrepaidAsync(int CancelAmount, string CancelReason, string PrepaidCardCode, string PrepaidUsageId)
        {
            try
            {
                //1. 기본적인 값 검증
                if (CancelAmount <= 0)
                {
                    ViewData["Error"] = "취소할 개수는 1개 이상이어야 합니다.";
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(CancelReason))
                {
                    ViewData["Error"] = "취소 사유를 입력해 주세요.";
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(PrepaidCardCode))
                {
                    ViewData["Error"] = "선불권 코드가 누락되었습니다.(서버 문제)";
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(PrepaidUsageId))
                {
                    ViewData["Error"] = "사용 기록 ID가 누락되었습니다.(서버 문제)";
                    return Page();
                }

                decimal cancelDecimal = CancelAmount; //미리 int형을 decimal로 변환(DB기준에 맞춤)

                //2.사용한 개수 이상으로 취소할 경우를 검증
                var usageAmount = await cardRepo.GetChangeAmountByUsageIdAsync(PrepaidUsageId);
                if (usageAmount == null)
                {
                    ViewData["Error"] = "해당 사용 기록을 찾을 수 없습니다.";
                    return Page();
                }

                if (cancelDecimal > usageAmount.Value)
                {
                    ViewData["Error"] = "취소 개수가 사용 개수를 초과했습니다.";
                    return Page();
                }

                //3.로그 기록용 Usage객체 생성
                if (HttpContext.Items.TryGetValue("Employee", out var employeeObj))
                {
                    CurrentEmployee = employeeObj as Employee;
                }
                if (CurrentEmployee == null)
                {
                    ViewData["Error"] = "직원 정보를 찾을 수 없습니다. 다시 로그인 해 주세요.(서버 문제)";
                    return Page();
                }

                string Note = $"처리자:{CurrentEmployee.Name}, 사유:{CancelReason}, 복구개수:{CancelAmount}";
                var UsageLog = new PrepaidCardUsage
                {
                    PrepaidCardId = 0, //내부에서 따로 조회해서 정합성 유지
                    ActionType = "RESTORE",
                    ChangeAmount = CancelAmount,
                    UsageNote = Note,
                    StoreCode = CurrentEmployee.Store,
                    UsedAt = DateTime.UtcNow,
                };

                PrepaidCardSummary result = await cardRepo.RestorePrepaidCardUsageAsync(PrepaidUsageId, cancelDecimal, PrepaidCardCode, UsageLog);
                var RefundEnvelope = new Envelope
                {
                    KakaoAlert = new SendKakaoAlertNotification
                    {
                        TemplateTitle = "선불권 사용 취소 알림",
                        Receiver = result.PurchaserContact,
                        Variables =
                    {
                        { "고객명", result.PurchaserName },
                        { "취소사유", CancelReason }, // 사용자가 입력한 값
                        { "환불개수", CancelAmount.ToString()},
                        { "잔여개수", result.RemainingValue.ToString("0")},
                        { "처리일시", PrintCurrentDate.PrintDate() ?? "" },
                        { "매장전화번호",StoreService.GetStorePhoneNumber(CurrentEmployee.Store)}
                    }
                    }
                };
                await sender.SendMessageAsync(RefundEnvelope);

                return NoticeService.RedirectWithNotice(HttpContext, $"{result.PurchaserName} 고객님의 선불권 {CancelAmount}개가 취소처리 되었습니다.", "/RefundPrepaid");
            }
            catch (Exception e)
            {
                Console.WriteLine("선불권 취소중 에러 발생" + e);
                return NoticeService.RedirectWithNotice(HttpContext, "선불권 취소중 에러 발생. 취소가 반영되지 않습니다", "/Home");
            }

        }

        public IActionResult OnPostSearchPrepaidDetail(string PhoneNumber)
        {
            try
            {
                if (!ValidDefaultAttribute.IsValidPhoneNumber(PhoneNumber, out var error))
                {
                    ViewData["Error"] = error;
                    return Page();
                }

                TempData["PhoneNumber"] = PhoneNumber;
                return RedirectToPage("/SearchPrepaidDetail/Index");
            }
            catch (Exception e)
            {
                Console.WriteLine("상세 기록 조회에서 예상치 못한 오류 발생" + e);
                return NoticeService.RedirectWithNotice(HttpContext, "상세 기록 조회과정에서 예상치 못한 오류가 발생 하였습니다", "/Home");
            }
        }

    }

}