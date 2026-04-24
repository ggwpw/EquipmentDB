using EquipmentDB.Data;
using EquipmentDB.Helpers;
using EquipmentDB.Models;

namespace EquipmentDB.Services;

public static class LogService
{
    public static void Log(string eventType, string description, string? table = null, int? recordId = null)
    {
        try
        {
            using var ctx = new AppDbContext();
            ctx.EventLog.Add(new EventLogEntry
            {
                UserId = CurrentSession.User?.Id,
                EventType = eventType,
                Description = description,
                TableName = table,
                RecordId = recordId
            });
            ctx.SaveChanges();
        }
        catch { }
    }
}
