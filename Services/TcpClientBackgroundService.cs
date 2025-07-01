using System.Net.Sockets;
using LTS.Configuration;
namespace LTS.Services;

using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CommsProto;

public class TcpClientBackgroundService : BackgroundService
{
    private readonly ITcpConnectionService _connectionService;
    private readonly ProtoHandler _protoHandler;

    public TcpClientBackgroundService(ITcpConnectionService connectionService, ProtoHandler protoHandler)
    {
        _connectionService = connectionService;
        _protoHandler = protoHandler;
    }

    private TcpClient? _client;
    private NetworkStream? _stream;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(EnvConfig.WatchTowerIp, 4000);
                _stream = _client.GetStream();

                Console.WriteLine("Watch Tower 서버에 연결 성공");

                _connectionService.SetStream(_stream);

                await SendAuthMessageAsync(_stream);// 인증 발송

                await ReceiveLoopAsync(_stream, stoppingToken); // 수신 루프

                Console.WriteLine("연결이 끊어졌습니다. 재연결 시도 중...");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("애플리케이션 종료 요청으로 TCP 서비스 중단됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"연결 실패 또는 예외 발생: {ex}");
            }

            // 재연결 대기
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ReceiveLoopAsync(NetworkStream stream, CancellationToken token)
    {

        // 1) 4바이트(길이) 읽기
        byte[] lengthPrefix = new byte[4];
        while (!token.IsCancellationRequested)
        {
            // 1-1) 길이(prefix) 먼저 읽기
            int readBytes = await stream.ReadAsync(lengthPrefix, 0, 4, token);
            if (readBytes == 0)
            {
                Console.WriteLine("서버가 연결을 닫았습니다.");
                break;
            }
            if (readBytes < 4)
            {
                // 도중에 연결이 끊기거나 데이터가 덜 왔을 때 간단 예외 처리
                throw new Exception("길이 필드를 전부 읽지 못했습니다.");
            }

            // (참고) C# 기본은 Little Endian이므로, 네트워크(Byte-순서)가 Big Endian이라면 바이트 순서를 바꿔야 한다.
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthPrefix);
            int messageLength = BitConverter.ToInt32(lengthPrefix, 0);

            // 2) 본문(payload) 읽기
            byte[] payloadBytes = new byte[messageLength];
            int totalRead = 0;
            while (totalRead < messageLength)
            {
                int chunkRead = await stream.ReadAsync(
                    payloadBytes, totalRead, messageLength - totalRead, token);
                if (chunkRead == 0)
                    throw new Exception("메시지 바디를 모두 읽지 못했습니다.");
                totalRead += chunkRead;
            }

            // 3) 프로토버프 역직렬화 및 핸들러 호출
            await ProcessEnvelope(payloadBytes);
        }
    }

    public override void Dispose()
    {
        _stream?.Dispose();
        _client?.Close();
        base.Dispose();

        GC.SuppressFinalize(this);//GC에 dispose 명시
    }


    private async Task ProcessEnvelope(byte[] payloadBytes)
    {
        // 3-1) 프로토버프 역직렬화
        Envelope envelope;
        try
        {
            envelope = Envelope.Parser.ParseFrom(payloadBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"프로토버프 파싱 오류: {ex.Message}");
            return;
        }

        // 3-2) 받은 메시지를 실제 핸들러로 전달
        await DispatchToHandler(envelope);
    }

    private async Task DispatchToHandler(Envelope envelope)
    {
        switch (envelope.PayloadCase)
        {
            case Envelope.PayloadOneofCase.Ping:
                _protoHandler.HandlePing(envelope.Ping);
                break;
            case Envelope.PayloadOneofCase.Pong:
                _protoHandler.HandlePong(envelope.Pong);
                break;
            case Envelope.PayloadOneofCase.Status:
                _protoHandler.HandleServerStatus(envelope.Status);
                break;
            case Envelope.PayloadOneofCase.KakaoAlert:
                _protoHandler.HandleSendKakaoAlertNotification(envelope.KakaoAlert);
                break;
            case Envelope.PayloadOneofCase.Ntfy:
                _protoHandler.HandleNtfyNotification(envelope.Ntfy);
                break;
            case Envelope.PayloadOneofCase.Message:
                _protoHandler.HandleCommonMessage(envelope.Message);
                break;
            case Envelope.PayloadOneofCase.TermAgreed:
                await _protoHandler.HandleTermAgreed(envelope.TermAgreed);
                break;
            case Envelope.PayloadOneofCase.None:
            default:
                Console.WriteLine("알 수 없는 타입의 메시지가 들어왔습니다 또는 payload가 비어 있습니다.");
                break;
        }
    }

    private static string AuthKeyGen(string timeStamp)
    {
        string sharedSecret = EnvConfig.WatchTowerAuthSecret;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sharedSecret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(timeStamp));

        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private static async Task SendAuthMessageAsync(NetworkStream _stream)
    {
        if (_stream == null || !_stream.CanWrite)
        {
            Console.WriteLine("스트림이 유효하지 않습니다.");
            return;
        }
        var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        var authEnvelope = new Envelope
        {
            Auth = new TcpAuth
            {
                Key = AuthKeyGen(timeStamp),
                TimeStamp = long.Parse(timeStamp),
                ContainerNumber = "1"
            }
        };

        var bytesToSend = ProtoMessageBuilder.BuildEnvelopeMessage(authEnvelope);
        await _stream.WriteAsync(bytesToSend.AsMemory());
        await _stream.FlushAsync();
    }

}
