namespace LTS.Models;
public class Employee
{
    public string? Id { get; set; }                    // 고유 ID
    public string? Initials { get; set; }               // 이니셜 (예: AHG)
    public string? Password { get; set; }               // 비밀번호
    public bool IsPasswordChanged { get; set; }        // 비밀번호 변경 여부
    public string? Store { get; set; }                  // 근무 매장명 또는 매장 ID
    public string? Position { get; set; }               // 직책 (staff, manager, owner)
    public DateTime WorkStartDate { get; set; }        // 근무 시작일
    public DateTime CreatedAt { get; set; }            // 가입일
    public string? CreatedByMember { get; set; }        // 가입을 진행한 사람
}