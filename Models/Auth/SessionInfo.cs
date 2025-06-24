namespace LTS.Models;
public class SessionInfo
{
    public string Token { get; set; }
    public DateTime ExpireAt { get; set; }

    public SessionInfo(string token, DateTime expireAt)
    {
        Token = token;
        ExpireAt = expireAt;
    }
}