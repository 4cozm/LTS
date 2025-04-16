//DB Pool 연결을 작성한 코드

using LTS.Configuration;
using MySql.Data.MySqlClient;

namespace LTS.Data;

public static class DbManager
{
    private static string _connectionString = "";

    static DbManager()
    {
        _connectionString = $"Server={EnvConfig.MySqlIp};Uid={EnvConfig.MySqlUserName};Pwd={EnvConfig.MySqlPassword};Pooling=true;";
    }

    public static MySqlConnection? GetConnection()
    {
        try
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"DB 연결 실패: {ex.Message}");
            
            return null;
        }
    }

}