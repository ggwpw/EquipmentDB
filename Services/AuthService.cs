using EquipmentDB.Data;
using EquipmentDB.Helpers;
using EquipmentDB.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipmentDB.Services;

public static class AuthService
{
    public const int MaxAttempts = 3;
    public enum LoginResult { Success, InvalidCredentials, AccountLocked }
    public record LoginResponse(LoginResult Result, int AttemptsLeft = 0);

    public static LoginResponse Login(string login, string password)
    {
        using var ctx = new AppDbContext();
        var user = ctx.Users.Include(u => u.Staff).FirstOrDefault(u => u.Login == login);

        if (user == null) { WriteHistory(null, false); return new(LoginResult.InvalidCredentials, MaxAttempts); }
        if (!user.IsActive) { WriteHistory(user.Id, false); return new(LoginResult.AccountLocked, 0); }

        bool ok;
        try { ok = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash); }
        catch { ok = false; }

        if (!ok)
        {
            user.FailedAttempts++;
            int left = MaxAttempts - user.FailedAttempts;
            if (left <= 0) { user.IsActive = false; ctx.SaveChanges(); WriteHistory(user.Id, false); return new(LoginResult.AccountLocked, 0); }
            ctx.SaveChanges(); WriteHistory(user.Id, false);
            return new(LoginResult.InvalidCredentials, left);
        }

        user.FailedAttempts = 0;
        ctx.SaveChanges();
        CurrentSession.User = user;
        WriteHistory(user.Id, true);
        return new(LoginResult.Success);
    }

    // DEV ONLY — пересоздаёт хэши через BCrypt.Net и снимает блокировки
    public static void DevResetPasswords()
    {
        using var ctx = new AppDbContext();
        var defaults = new Dictionary<string, string>
        {
            ["admin"] = "admin123", ["operator"] = "oper123", ["observer"] = "obs123"
        };
        foreach (var u in ctx.Users.ToList())
        {
            u.IsActive = true; u.FailedAttempts = 0;
            if (defaults.TryGetValue(u.Login, out var pwd))
                u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(pwd);
        }
        ctx.SaveChanges();
    }

    public static bool ChangePassword(int userId, string current, string newPwd)
    {
        using var ctx = new AppDbContext();
        var user = ctx.Users.Find(userId); if (user == null) return false;
        try { if (!BCrypt.Net.BCrypt.Verify(current, user.PasswordHash)) return false; } catch { return false; }
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPwd);
        ctx.SaveChanges();
        LogService.Log("Смена пароля", $"Пользователь {user.Login} сменил пароль", "users", userId);
        return true;
    }

    private static void WriteHistory(int? userId, bool success)
    {
        try
        {
            using var ctx = new AppDbContext();
            ctx.LoginHistory.Add(new LoginHistory { UserId = userId, PcName = Environment.MachineName, Success = success });
            ctx.SaveChanges();
        }
        catch { }
    }
}
