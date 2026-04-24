namespace EquipmentDB.Models;
public class Equipment
{
    public int Id { get; set; }
    public string InventoryNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TypeId { get; set; }
    public EquipmentType Type { get; set; } = null!;
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public DateOnly ArrivalDate { get; set; }
    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;
    public string? Specs { get; set; }
    public string? PhotoPath { get; set; }
}
