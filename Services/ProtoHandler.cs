namespace LTS.Services;

using CommsProto;

// Envelope 메시지에 정의된 각 메시지 타입을 처리할 핸들러들
public static class ProtoHandler
{
    public static void HandlePing(Ping pingMsg)
    {
        // Ping 메시지 처리 로직
        Console.WriteLine($"[HandlePing] From: {pingMsg.From}, Timestamp: {pingMsg.Timestamp}");
    }

    public static void HandlePong(Pong pongMsg)
    {
        // Pong 메시지 처리 로직
        Console.WriteLine($"[HandlePong] From: {pongMsg.From}, Timestamp: {pongMsg.Timestamp}");
    }

    public static void HandleServerStatus(ServerStatus statusMsg)
    {
        // ServerStatus 메시지 처리 로직
        Console.WriteLine($"[HandleServerStatus] CPU: {statusMsg.CpuUsage}, Memory: {statusMsg.MemoryUsage}, Hostname: {statusMsg.Hostname}");
    }

    public static void HandleSendKakaoAlertNotification(SendKakaoAlertNotification kakaoMsg)
    {
        // 카카오 알림 메시지 처리 로직
        Console.WriteLine($"[HandleKakaoAlert] TemplateTitle: {kakaoMsg.TemplateTitle}, Receiver: {kakaoMsg.Receiver}, Variables: {kakaoMsg.Variables}");
    }

    public static void HandleNtfyNotification(NtfyNotification ntfyMsg)
    {
        // ntfy 알림 메시지 처리 로직
        Console.WriteLine($"[HandleNtfy] Topic: {ntfyMsg.Topic}, Title: {ntfyMsg.Title}, Message: {ntfyMsg.Message}");
    }

    public static void HandleCommonMessage(CommonMessage message)
    {
        Console.WriteLine($"Watch Tower 서버 : {message.Message}");
    }
}
