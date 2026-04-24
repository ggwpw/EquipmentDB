namespace EquipmentDB.Models;
public class Staff
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int? RoomId { get; set; }
    public Room? Room { get; set; }
    public List<User> Users { get; set; } = [];
    public override string ToString() => FullName;
}
