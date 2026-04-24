namespace EquipmentDB.Models;
public class Status
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#000000";
    public List<Equipment> Equipment { get; set; } = [];
    public override string ToString() => Name;
}
