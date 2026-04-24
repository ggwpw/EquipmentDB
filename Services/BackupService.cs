using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using EquipmentDB.Data;

namespace EquipmentDB.Services;

public static class BackupService
{
    public static string CreateBackup(string dir)
    {
        Directory.CreateDirectory(dir);
        var cs       = DatabaseConfig.ConnectionString;
        var server   = Extract(cs, "Server")   ?? "localhost";
        var port     = Extract(cs, "Port")     ?? "3306";
        var database = Extract(cs, "Database") ?? "equipment_db";
        var user     = Extract(cs, "User")     ?? "root";
        var password = Extract(cs, "Password") ?? "";

        var file = Path.Combine(dir, $"backup_{database}_{DateTime.Now:yyyyMMdd_HHmmss}.sql");
        var psi = new ProcessStartInfo
        {
            FileName = "mysqldump",
            Arguments = $"--host={server} --port={port} --user={user} --password={password} {database}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi)!;
        File.WriteAllText(file, proc.StandardOutput.ReadToEnd());
        proc.WaitForExit();
        LogService.Log("Резервное копирование", $"Создан бэкап: {Path.GetFileName(file)}");
        return file;
    }

    private static string? Extract(string cs, string key)
    {
        var m = Regex.Match(cs, $@"(?i){key}\s*=\s*([^;]+)");
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }
}
