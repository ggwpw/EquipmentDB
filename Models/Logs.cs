namespace EquipmentDB.Models;
public class EventLogEntry
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TableName { get; set; }
    public int? RecordId { get; set; }
    public DateTime EventTime { get; set; } = DateTime.Now;
}
public class LoginHistory
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public DateTime LoginTime { get; set; } = DateTime.Now;
    public string? PcName { get; set; }
    public bool Success { get; set; }
}
