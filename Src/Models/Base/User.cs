namespace LTS.Models;

public class User
{
    public int Id { get; set; }
    public string Password { get; set; } = "";
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
    public bool DirtyFlag { get; set; }
}
