using System.IO;
using System.Text.Json;

namespace EquipmentDB.Data;

public static class DatabaseConfig
{
    private static string? _cs;
    public static string ConnectionString
    {
        get
        {
            if (_cs != null) return _cs;
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            var doc = JsonDocument.Parse(File.ReadAllText(path));
            _cs = doc.RootElement.GetProperty("ConnectionString").GetString()!;
            return _cs;
        }
    }
}
