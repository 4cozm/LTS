//프로토버프를 위한 직렬화 함수
using CommsProto;



using Google.Protobuf;
public static class ProtoMessageBuilder
{
    public static byte[] BuildEnvelopeMessage(Envelope envelope)
    {
        byte[] bodyBytes = envelope.ToByteArray();

        byte[] lengthPrefix = BitConverter.GetBytes(bodyBytes.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthPrefix); // Big Endian

        byte[] fullMessage = new byte[lengthPrefix.Length + bodyBytes.Length];
        Buffer.BlockCopy(lengthPrefix, 0, fullMessage, 0, lengthPrefix.Length);
        Buffer.BlockCopy(bodyBytes, 0, fullMessage, lengthPrefix.Length, bodyBytes.Length);

        return fullMessage;
    }
}
