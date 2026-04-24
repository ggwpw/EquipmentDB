using EquipmentDB.Data;
using EquipmentDB.Helpers;
using EquipmentDB.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipmentDB.Services;

public static class AuthService
{
    public enum LoginResult { Success, InvalidCredentials, AccountLocked }

    public static LoginResult Login(string login, string password)
    {
        using var ctx = new AppDbContext();
        var user = ctx.Users.Include(u => u.Staff).FirstOrDefault(u => u.Login == login);

        if (user == null) { WriteHistory(null, false); return LoginResult.InvalidCredentials; }
        if (!user.IsActive) { WriteHistory(user.Id, false); return LoginResult.AccountLocked; }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.FailedAttempts++;
            if (user.FailedAttempts >= 3) user.IsActive = false;
            ctx.SaveChanges();
            WriteHistory(user.Id, false);
            return user.IsActive ? LoginResult.InvalidCredentials : LoginResult.AccountLocked;
        }

        user.FailedAttempts = 0;
        ctx.SaveChanges();
        CurrentSession.User = user;
        WriteHistory(user.Id, true);
        return LoginResult.Success;
    }

    public static bool ChangePassword(int userId, string current, string newPwd)
    {
        using var ctx = new AppDbContext();
        var user = ctx.Users.Find(userId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(current, user.PasswordHash)) return false;
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
