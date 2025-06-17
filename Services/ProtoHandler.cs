namespace LTS.Services;

using CommsProto;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

public class ProtoHandler(RedisService redisService, IHubContext<StatusHub> hubContext)
{
    private readonly RedisService _redisService = redisService;

    public void HandlePing(Ping pingMsg)
    {
        Console.WriteLine($"[HandlePing] From: {pingMsg.From}, Timestamp: {pingMsg.Timestamp}");
    }

    public void HandlePong(Pong pongMsg)
    {
        Console.WriteLine($"[HandlePong] From: {pongMsg.From}, Timestamp: {pongMsg.Timestamp}");
    }

    public void HandleServerStatus(ServerStatus statusMsg)
    {
        Console.WriteLine($"[HandleServerStatus] CPU: {statusMsg.CpuUsage}, Memory: {statusMsg.MemoryUsage}, Hostname: {statusMsg.Hostname}");
    }

    public void HandleSendKakaoAlertNotification(SendKakaoAlertNotification kakaoMsg)
    {
        Console.WriteLine($"[HandleKakaoAlert] TemplateTitle: {kakaoMsg.TemplateTitle}, Receiver: {kakaoMsg.Receiver}, Variables: {kakaoMsg.Variables}");
    }

    public void HandleNtfyNotification(NtfyNotification ntfyMsg)
    {
        Console.WriteLine($"[HandleNtfy] Topic: {ntfyMsg.Topic}, Title: {ntfyMsg.Title}, Message: {ntfyMsg.Message}");
    }

    public void HandleCommonMessage(CommonMessage message)
    {
        Console.WriteLine($"Watch Tower 서버 : {message.Message}");
    }

    public async Task HandleTermAgreed(TermAgreed termAgreed)
    {
        try
        {
            var db = _redisService.GetDatabase();
            var key = $"consent:{termAgreed.PhoneNumber}";

            // 기존 데이터 조회
            var existingJson = await db.StringGetAsync(key);
            if (!existingJson.HasValue)
            {
                Console.WriteLine($"⚠️ 인증서 전송 없이 약관에만 동의: {termAgreed.PhoneNumber}");
                await hubContext.Clients.All.SendAsync("ConsentWithoutAuth", termAgreed.PhoneNumber);
                return;
            }

            ConsentData existing = existingJson.HasValue
                ? JsonSerializer.Deserialize<ConsentData>(existingJson!)!
                : new ConsentData { PhoneNumber = termAgreed.PhoneNumber };

            // 병합
            existing.Name = termAgreed.Name;
            existing.TermVersion = termAgreed.TermVersion;
            existing.AgreedAt = DateTime.UtcNow;

            // 다시 저장
            var newJson = JsonSerializer.Serialize(existing);
            await db.StringSetAsync(key, newJson, TimeSpan.FromHours(24));
            await hubContext.Clients.All.SendAsync("ConsentReceived"); //자동 새로고침 전파
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandleTermAgreed] Redis 저장 실패: {ex.Message}");
        }
    }
}
