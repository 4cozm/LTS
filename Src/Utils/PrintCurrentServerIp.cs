using System.Net;
namespace LTS.Utils;

public static class PrintCurrentIp
{
    public static string GetLocalIPAddress()
    {
        string hostName = Dns.GetHostName();
        var addresses = Dns.GetHostAddresses(hostName);

        var ipv4 = addresses.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

        return ipv4?.ToString() ?? "IPv4 주소를 찾을 수 없습니다.";
    }
}
