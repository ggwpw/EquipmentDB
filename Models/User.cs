namespace EquipmentDB.Models;
public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "observer";
    public int? StaffId { get; set; }
    public Staff? Staff { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public byte FailedAttempts { get; set; }
    public List<EventLogEntry> EventLogs { get; set; } = [];
    public List<LoginHistory> LoginHistories { get; set; } = [];
}
