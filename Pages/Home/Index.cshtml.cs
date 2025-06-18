using LTS.Base;
using LTS.Services;
using System.Text.Json;
using LTS.Models;

namespace LTS.Pages.Home
{
    public class IndexModel(RedisService redis) : BasePageModel
    {
        public List<ConsentData> Customers { get; private set; } = [];

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
    }
}