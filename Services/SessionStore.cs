using System.Collections.Concurrent;
using LTS.Models;

namespace LTS.Services;

public class SessionStore
{
    private static readonly ConcurrentDictionary<string, (Employee Employee, DateTime ExpireAt)> _sessions = [];

public static string CreateSession(Employee employee, DateTime expireAt)
{
    var token = Guid.NewGuid().ToString();
    _sessions[token] = (employee, expireAt);
    return token;
}

    public static Employee? GetSession(string token)
    {
        if (_sessions.TryGetValue(token, out var session))
        {
            if (session.ExpireAt > DateTime.UtcNow)
                return session.Employee;

            _sessions.TryRemove(token,out _);
        }
        return null;
    }
}