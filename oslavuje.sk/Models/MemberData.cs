namespace oslavuje.sk.Models;

public class MemberData
{
    public int Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserInput { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}