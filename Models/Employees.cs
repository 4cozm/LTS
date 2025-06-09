namespace LTS.Models;

public class Employee
{
    public override string ToString()
    {
        return $"[Employee] Id: {Id}, Initials: {Initials}, Name: {Name}, Role: {RoleName}, Store: {Store}";
    }
    public int Id { get; set; }                         //DB 고유키 (읽기용)
    public string? Name { get; set; }                  // 이름 (예: 안홍걸)
    public string? Initials { get; set; }               // 이니셜 (예: AHG)
    public string? PhoneNumber { get; set; }            // 전화번호 (예: 010-1234-5678)
    public string? Password { get; set; }               // 비밀번호
    public bool IsPasswordChanged { get; set; }      //초기 패스워드 변경 여부
    public string? Store { get; set; }                  // 근무 매장명 또는 매장 ID
    public string? RoleName { get; set; }               // 직책 (staff, manager, owner)
    public DateTime WorkStartDate { get; set; }        // 근무 시작일
    public DateTime CreatedAt { get; set; }            // 가입일
    public string? CreatedByMember { get; set; }        // 가입을 진행한 사람
}