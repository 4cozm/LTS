using System.Collections.Concurrent;
using LTS.Models;

namespace LTS.Services;

public class SessionStore
{
    private static readonly ConcurrentDictionary<string, (Employee Employee, DateTime ExpireAt)> _sessions = [];

    public static SessionInfo CreateSession(Employee employee)
    {
        var expireAt = DateTime.UtcNow.AddHours(4); // 4시간 후 만료
        var token = Guid.NewGuid().ToString();
        _sessions[token] = (employee, expireAt);
        return new SessionInfo(token, expireAt);
    }
    public static Employee? GetSession(string token)
    {
        if (_sessions.TryGetValue(token, out var session))
        {
            if (session.ExpireAt > DateTime.UtcNow)
                return session.Employee;

            _sessions.TryRemove(token, out _);
        }
        return null;
    }

    public static void RemoveSession(string token)
    {
        _sessions.TryRemove(token, out _);
    }
}
