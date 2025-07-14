using StackExchange.Redis;


namespace LTS.Services;

public class RedisService(IConnectionMultiplexer multiplexer)
{
    public IDatabase GetDatabase(int db = -1) => multiplexer.GetDatabase(db);
    public IServer GetServer()
    {
        var endpoint = multiplexer.GetEndPoints().First();
        return multiplexer.GetServer(endpoint);
    }
}
