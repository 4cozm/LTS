using Dapper;
using LTS.Models;

namespace LTS.Data.Repository;
public class EmployeeRepository
{
    public Employee? GetEmployeeByInitial(string initials)
    {
        using var conn = DbManager.GetConnection();
        if (conn == null) return null;

        string query = "SELECT * FROM employees WHERE initials = @initials";
        return conn.QueryFirstOrDefault<Employee>(query, new { initials });
    }
}