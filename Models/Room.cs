namespace EquipmentDB.Models;
public class Room
{
    public int Id { get; set; }
    public string Cabinet { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? PhotoPath { get; set; }
    public List<Staff> Staff { get; set; } = [];
    public List<Equipment> Equipment { get; set; } = [];
    public override string ToString() => $"{Cabinet} — {Name}";
}
