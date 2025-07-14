namespace LTS.Models;

public class Employee
{
    public override string ToString()
    {
        return $"[Employee] Id: {Id}, Initials: {Initials}, Name: {Name}, Role: {RoleName}, Store: {Store}, PhoneNumber: {PhoneNumber}";
    }
    public int Id { get; set; }
    public string Initials { get; set; } = "";
    public string Name { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Password { get; set; } = "";
    public bool IsPasswordChanged { get; set; }
    public string Store { get; set; } = "";
    public string RoleName { get; set; } = "";
    public DateTime WorkStartDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByMember { get; set; }
}
