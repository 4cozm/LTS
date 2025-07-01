using CommsProto;
using Mysqlx;

namespace LTS.Services;

public class SendProtoMessage
{
    private readonly ITcpConnectionService _tcp;

    public SendProtoMessage(ITcpConnectionService tcp)
    {
        _tcp = tcp;
    }

    /*
    다 만들어진 envelope를 전달하면 메세지 전송까지 해줌
    */
    public async Task SendMessageAsync(Envelope envelope)
    {
        try
        {
            var stream = _tcp.GetStream();
            if (stream == null)
            {
                Console.WriteLine("TCP 연결이 끊겨 메시지를 전송할 수 없습니다.");
                return;
            }
        ;

            var bytes = ProtoMessageBuilder.BuildEnvelopeMessage(envelope);
            await stream.WriteAsync(bytes.AsMemory());
            await stream.FlushAsync();
            Console.WriteLine("Watch Tower 로 메세지 전송 완료");
        }
        catch (Exception e)
        {
            Console.WriteLine("SendMessageAsync에서 에러 발생", e.Message);
            throw new Exception(e.Message);
        }
    }
}
