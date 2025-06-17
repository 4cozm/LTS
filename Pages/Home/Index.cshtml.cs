using LTS.Base;
using LTS.Services;
using System.Text.Json;

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

            foreach (var key in keys)
            {
                var json = await db.StringGetAsync(key);
                if (json.IsNullOrEmpty) continue;

                var data = JsonSerializer.Deserialize<ConsentData>(json!)!;
                customers.Add(data);
            }

            Customers = customers
                .OrderBy(c => c.AgreedAt ?? c.SentAt ?? DateTime.MinValue)
                .ToList();
        }
    }
}