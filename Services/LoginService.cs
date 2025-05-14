using LTS.Data.Repository;
using LTS.Models;



namespace LTS.Services;
public class LoginService
{
    private readonly EmployeeRepository _repo;



    public LoginService(EmployeeRepository repo)
    {
        _repo = repo;
    }
    public SessionInfo TryLogin(string initials, string password)
    {
        var employee = _repo.GetEmployeeByInitial(initials);

        if (employee == null || employee.Password == null)
            throw new UnauthorizedAccessException("해당 사용자 정보를 찾을 수 없습니다.");

        if (!VerifyPassword(password, employee.Password))
            throw new UnauthorizedAccessException("비밀번호가 잘못되었습니다.");

        return SessionStore.CreateSession(employee);
    }

    private static bool VerifyPassword(string input, string inDbPassword)
    {
        return BCrypt.Net.BCrypt.Verify(input, inDbPassword);
    }

    public static (bool isValid, Employee? employee) TryGetValidEmployeeFromToken(string token)
    {
        var employee = SessionStore.GetSession(token);
        return (employee != null, employee);
    }
}