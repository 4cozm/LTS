using LTS.Base;
using LTS.Services;
using System.Text.Json;
using LTS.Models;
using Microsoft.AspNetCore.Mvc;
using LTS.Utils;
using LTS.Data.Repository;
using CommsProto;

namespace LTS.Pages.Home
{
    public class IndexModel(RedisService redis, PrepaidCardRepository repo, SendProtoMessage sender) : BasePageModel
    {
        public List<ConsentData> Customers { get; private set; } = [];

        public Employee? CurrentEmployee { get; private set; }

        public async Task OnGetAsync()
        {
            var db = redis.GetDatabase();
            var server = redis.GetServer();

            var keys = server.Keys(pattern: "consent:*");
            var customers = new List<ConsentData>();

            // 현재 직원 정보 가져오기
            var currentEmployee = HttpContext.Items["Employee"] as Employee;
            if (currentEmployee == null)
            {
                // 인증되지 않은 경우나 오류 처리
                Customers = [];
                return;
            }

            foreach (var key in keys)
            {
                var json = await db.StringGetAsync(key);
                if (json.IsNullOrEmpty) continue;

                var data = JsonSerializer.Deserialize<ConsentData>(json!)!;

                // StoreCode가 현재 직원의 점포와 일치하는 경우만 추가
                if (data.StoreCode == currentEmployee.Store)
                {
                    customers.Add(data);
                }
            }

            Customers = customers
                .OrderBy(c => c.AgreedAt ?? c.SentAt ?? DateTime.MinValue)
                .ToList();
        }

        public IActionResult OnPostDeleteCustomer(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return BadRequest();

            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
            var key = $"consent:{digitsOnly}";

            var db = redis.GetDatabase();

            db.KeyDelete(key);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompletePaymentAsync(string phoneNumber, string product, string receipt)
        {
            try
            {
                if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(product) || string.IsNullOrEmpty(receipt))
                    return NoticeService.RedirectWithNotice(HttpContext, "저장되지 않은 고객 정보가 있습니다(시스템 문제)", "/Home");

                var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
                var redisKey = $"consent:{digitsOnly}";

                var db = redis.GetDatabase();
                var json = db.StringGet(redisKey);
                if (json.IsNullOrEmpty)
                    return NoticeService.RedirectWithNotice(HttpContext, "고객 정보를 Redis에서 찾을 수 없습니다(시스템 문제)", "/Home");

                var consentData = JsonSerializer.Deserialize<ConsentData>(json!)!;
                if (string.IsNullOrEmpty(consentData.Name) ||
                    string.IsNullOrEmpty(consentData.TermVersion) ||
                    string.IsNullOrEmpty(consentData.StoreCode))
                {
                    return NoticeService.RedirectWithNotice(HttpContext, "고객 정보가 불완전 합니다(시스템 문제)", "/Home");
                }

                var issuedAt = DateTime.UtcNow;
                var selectedProduct = PrepaidProductCatalog.GetByCode(product);
                if (selectedProduct == null)
                    return NoticeService.RedirectWithNotice(HttpContext, product + "는 구매할 수 없는 상품 타입 입니다.", "/Home");
                var displayName = selectedProduct.DisplayName;
                var prepaidCardPrice = selectedProduct.Price.ToString("N0"); ;
                var initialValue = selectedProduct.Count;

                //생성 시작
                if (HttpContext.Items.TryGetValue("Employee", out var employeeObj))
                {
                    CurrentEmployee = employeeObj as Employee;
                }

                var card = new PrepaidCard
                {
                    Code = PrepaidCardUtil.GenerateCardCode(),
                    Type = "COUNT", // 현재는 갯수제만 사용
                    InitialValue = initialValue,
                    RemainingValue = initialValue,
                    IssuedAt = issuedAt,
                    ExpiresAt = issuedAt.AddMonths(6),
                    IsActive = true,
                    PurchaserName = consentData.Name,
                    PurchaserContact = digitsOnly,
                    StoreCode = consentData.StoreCode,
                    Notes = $"영수증: {receipt} | 발송: {consentData.SentAt:yyyy-MM-dd HH:mm} | " +
                        $"동의: {consentData.AgreedAt:yyyy-MM-dd HH:mm} | 버전: {consentData.TermVersion} | " +
                        $"담당자: {CurrentEmployee?.Name}"
                };

                var createdCard = repo.CreatePrepaidCardWithUsage(card, new PrepaidCardUsage
                {
                    ActionType = "TEST", // 초기 등록
                    ChangeAmount = 0,
                    UsageNote = "초기 구매 자동 등록",
                    UsedAt = DateTime.UtcNow
                });

                db.KeyDelete(redisKey);

                var PrepaidEnvelope = new Envelope
                {
                    KakaoAlert = new SendKakaoAlertNotification
                    {
                        TemplateTitle = "선불권 구매 알림",
                        Receiver = digitsOnly,
                        Variables =
                    {
                        {"고객명",consentData.Name},
                        {"상품명",displayName},
                        {"결제금액",prepaidCardPrice},
                        {"유효기간",PrintCurrentDate.PrintDate(DateTime.UtcNow.AddMonths(6))}, //유효기간 6개월
                        {"남은횟수",initialValue.ToString()},
                        {"매장전화번호",StoreService.GetStorePhoneNumber(CurrentEmployee!.Store!)}
                    }
                    }
                };
                await sender.SendMessageAsync(PrepaidEnvelope);


                return NoticeService.RedirectWithNotice(HttpContext, "구매 정보가 성공적으로 처리되었습니다.", "/Home");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[결제처리 오류] {ex.Message}");
                return NoticeService.RedirectWithNotice(HttpContext, "결제 처리 오류 발생.", "/Home");
            }
        }


    }
}