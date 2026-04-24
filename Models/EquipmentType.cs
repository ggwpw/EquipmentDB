namespace EquipmentDB.Models;
public class EquipmentType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<Equipment> Equipment { get; set; } = [];
    public override string ToString() => Name;
}
