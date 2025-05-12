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

    public Employee? CreateEmployee(Employee employee)
    {
        try
        {
            using var conn = DbManager.GetConnection();
            if (conn == null)
            {
                throw new InvalidOperationException("DB 연결에 실패했습니다.");
            }

            string query = @"
            INSERT INTO employees (initials, password, store, role_name, work_start_date, created_by_member) 
            VALUES (@Initials, @Password, @Store, @RoleName, @WorkStartDate, @CreatedByMember)";

            var result = conn.Execute(query, new
            {
                employee.Initials,
                employee.Password,
                employee.Store,
                employee.RoleName,
                employee.WorkStartDate,
                employee.CreatedByMember
            });

            if (result > 0)
            {
                return employee;
            }

            throw new Exception("직원 생성에 실패했습니다.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"예외 발생: {ex.Message}");
            throw new InvalidOperationException("직원 생성 중 오류가 발생했습니다.", ex);
        }
    }

}