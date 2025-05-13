namespace LTS.Models;
public class Employee
{
    public string? Name { get; set; }                  // 이름 (예: 안홍걸)
    public string? Initials { get; set; }               // 이니셜 (예: AHG)
    public string? Password { get; set; }               // 비밀번호
    public string? Store { get; set; }                  // 근무 매장명 또는 매장 ID
    public string? RoleName { get; set; }               // 직책 (staff, manager, owner)
    public DateTime WorkStartDate { get; set; }        // 근무 시작일
    public DateTime CreatedAt { get; set; }            // 가입일
    public string? CreatedByMember { get; set; }        // 가입을 진행한 사람
}