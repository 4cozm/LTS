using LTS.Data.Repository;
using Microsoft.IdentityModel.Tokens;
using LTS.Models;
using LTS.Configuration;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LTS.Services;
public class LoginService
{
    private readonly EmployeeRepository _repo;


    public LoginService(EmployeeRepository repo)
    {
        _repo = repo;
    }
    public string TryLogin(string initials, string password)
    {
        var employee = _repo.GetEmployeeByInitial(initials);

        if (employee == null || employee.Password == null)
        {
            throw new UnauthorizedAccessException("해당 사용자 정보를 찾을 수 없습니다.");  
        }

        if (!VerifyPassword(password, employee.Password))
        {
            throw new UnauthorizedAccessException("비밀번호가 잘못되었습니다.");
        }

        return GenerateToken(employee);
    }

    private static bool VerifyPassword(string input, string inDbPassword)
    {
        return BCrypt.Net.BCrypt.Verify(input, inDbPassword);
    }

    private string GenerateToken(Employee employee)
    {
        var expireAt = DateTime.UtcNow.AddHours(4);
        var sessionToken = SessionStore.CreateSession(employee, expireAt);


        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(EnvConfig.JwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("sessionToken", sessionToken),
        };
        var tokenDescriptor = new JwtSecurityToken(
            issuer: "LTS-server", // 나중에 실제 값으로 변경 예정
            audience: "LTS-app", //나중에 실제 값으로 변경 예정
            claims: claims,
            expires: expireAt,
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(tokenDescriptor);
    }
}