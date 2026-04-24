using System.Windows.Input;
using EquipmentDB.Helpers;
using EquipmentDB.Services;

namespace EquipmentDB.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private string _login = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    public string Login        { get => _login;        set => Set(ref _login, value); }
    public string ErrorMessage { get => _errorMessage; set => Set(ref _errorMessage, value); }
    public bool   IsLoading    { get => _isLoading;    set => Set(ref _isLoading, value); }

    public ICommand LoginCommand { get; }
    public event Action? LoginSucceeded;

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(DoLogin, _ => !string.IsNullOrWhiteSpace(Login) && !IsLoading);
    }

    private void DoLogin(object? param)
    {
        var password = param?.ToString() ?? string.Empty;
        IsLoading = true; ErrorMessage = string.Empty;
        var result = AuthService.Login(Login, password);
        IsLoading = false;
        switch (result)
        {
            case AuthService.LoginResult.Success:        LoginSucceeded?.Invoke(); break;
            case AuthService.LoginResult.AccountLocked:  ErrorMessage = "Учётная запись заблокирована."; break;
            default:                                     ErrorMessage = "Неверный логин или пароль."; break;
        }
    }
}
