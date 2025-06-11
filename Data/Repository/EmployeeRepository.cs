using Dapper;
using LTS.Models;
using MySql.Data.MySqlClient;
namespace LTS.Data.Repository;
/*
Repository 계층에서는 모든 예외를 InvalidOperationException으로 감싸서 던지는 책임을 진다

이 책임이 무너지면 PageModel에서 예외가 새어 나가거나 앱이 죽을 수 있음.
*/
public class EmployeeRepository
{
    public Employee? GetEmployeeByInitial(string initials)
    {
        using var conn = DbManager.GetConnection();
        if (conn == null) return null;

        string query = @"
        SELECT id AS Id,
            initials AS Initials,
            name AS Name,
            password AS Password,
            is_password_changed AS IsPasswordChanged,
            store AS Store,
            role_name AS RoleName,
            work_start_date AS WorkStartDate,
            phone_number AS PhoneNumber,
            created_at AS CreatedAt,
            created_by_member AS CreatedByMember
        FROM employees
        WHERE initials = @initials";

        return conn.QueryFirstOrDefault<Employee>(query, new { initials });
    }

    public Employee? CreateEmployee(Employee employee)
    {
        try
        {
            Console.WriteLine($"[LOG] CreateEmployee: {employee.Name}, {employee.CreatedByMember}");
            using var conn = DbManager.GetConnection();
            if (conn == null)
            {
                throw new InvalidOperationException("DB 연결에 실패했습니다.");
            }

            string query = @"
            INSERT INTO employees (name,phone_number,initials, password, store, role_name, work_start_date, created_by_member) 
            VALUES (@Name,@PhoneNumber,@Initials, @Password, @Store, @RoleName, @WorkStartDate, @CreatedByMember)";

            var result = conn.Execute(query, new
            {
                employee.Name,
                employee.PhoneNumber,
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
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            Console.Error.WriteLine($"[중복 데이터 오류] {ex.Message}");
            throw new InvalidOperationException("이미 존재하는 이니셜입니다.이름 뒤에 숫자를 붙여서 생성하세요 ex) AHG2,AGH3", ex);
        }
        catch (MySqlException ex)
        {
            Console.Error.WriteLine($"[MySQL 오류] {ex.Message}");
            throw new InvalidOperationException("DB 처리 중 오류가 발생했습니다.", ex);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[알 수 없는 오류] {ex.Message}");
            throw new InvalidOperationException("직원 생성 중 알 수 없는 오류가 발생했습니다.", ex);
        }
    }
    public void UpdatePassword(int employeeId, string newPasswordHash)
    {
        try
        {
            using var conn = DbManager.GetConnection();
            if (conn == null)
            {
                throw new InvalidOperationException("DB 연결에 실패했습니다.");
            }

            string query = @"
            UPDATE employees
            SET password = @Password,
                is_password_changed = true
            WHERE id = @Id";

            int affectedRows = conn.Execute(query, new
            {
                Password = newPasswordHash,
                Id = employeeId
            });

            if (affectedRows == 0)
            {
                throw new InvalidOperationException("비밀번호를 변경할 직원이 존재하지 않거나, 변경에 실패했습니다.");
            }
        }
        catch (MySqlException ex)
        {
            Console.Error.WriteLine($"[MySQL 오류] {ex.Message}");
            throw new InvalidOperationException("비밀번호 변경 중 DB 오류가 발생했습니다.", ex);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[알 수 없는 오류] {ex.Message}");
            throw new InvalidOperationException("비밀번호 변경 중 알 수 없는 오류가 발생했습니다.", ex);
        }
    }


}