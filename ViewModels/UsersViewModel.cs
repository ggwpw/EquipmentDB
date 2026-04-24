using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using EquipmentDB.Data;
using EquipmentDB.Helpers;
using EquipmentDB.Models;
using EquipmentDB.Services;
using Microsoft.EntityFrameworkCore;

namespace EquipmentDB.ViewModels;

public class UsersViewModel : BaseViewModel
{
    private ObservableCollection<User> _users = [];
    private User? _selected;

    public ObservableCollection<User> Users    { get => _users;    set => Set(ref _users, value); }
    public User?                      Selected { get => _selected; set => Set(ref _selected, value); }

    public ICommand AddCommand      { get; }
    public ICommand EditCommand     { get; }
    public ICommand ToggleCommand   { get; }
    public ICommand ResetPwdCommand { get; }
    public ICommand RefreshCommand  { get; }

    public UsersViewModel()
    {
        AddCommand      = new RelayCommand(_ => OpenDialog(null));
        EditCommand     = new RelayCommand(_ => OpenDialog(Selected),  _ => Selected != null);
        ToggleCommand   = new RelayCommand(_ => ToggleActive(),        _ => Selected != null);
        ResetPwdCommand = new RelayCommand(_ => ResetPassword(),       _ => Selected != null);
        RefreshCommand  = new RelayCommand(_ => Load());
        Load();
    }

    public void Load()
    {
        using var ctx = new AppDbContext();
        Users = new ObservableCollection<User>(ctx.Users.Include(u => u.Staff).OrderBy(u => u.Login).ToList());
    }

    private void OpenDialog(User? user) { var d = new Views.Dialogs.UserDialog(user); if (d.ShowDialog() == true) Load(); }

    private void ToggleActive()
    {
        if (Selected == null) return;
        using var ctx = new AppDbContext();
        var u = ctx.Users.Find(Selected.Id); if (u == null) return;
        u.IsActive = !u.IsActive;
        if (u.IsActive) u.FailedAttempts = 0;
        ctx.SaveChanges();
        LogService.Log(u.IsActive ? "Разблокировка" : "Блокировка", $"Пользователь {u.Login}", "users", u.Id);
        Load();
    }

    private void ResetPassword()
    {
        if (Selected == null) return;
        var dlg = new Views.Dialogs.ResetPasswordDialog(Selected.Login);
        if (dlg.ShowDialog() != true) return;
        using var ctx = new AppDbContext();
        var u = ctx.Users.Find(Selected.Id); if (u == null) return;
        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dlg.NewPassword);
        u.FailedAttempts = 0; u.IsActive = true;
        ctx.SaveChanges();
        LogService.Log("Сброс пароля", $"Сброшен пароль для {u.Login}", "users", u.Id);
        MessageBox.Show("Пароль успешно изменён.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        Load();
    }
}
