using System.Windows.Input;
using EquipmentDB.Helpers;
using EquipmentDB.Services;

namespace EquipmentDB.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private string _login = string.Empty;
    private string _errorMessage = string.Empty;
    private string _attemptsMessage = string.Empty;
    private bool _isLoading;

    public string Login           { get => _login;           set => Set(ref _login, value); }
    public string ErrorMessage    { get => _errorMessage;    set => Set(ref _errorMessage, value); }
    public string AttemptsMessage { get => _attemptsMessage; set => Set(ref _attemptsMessage, value); }
    public bool   IsLoading       { get => _isLoading;       set => Set(ref _isLoading, value); }

    public ICommand LoginCommand     { get; }
    public ICommand DevResetCommand  { get; }
    public event Action? LoginSucceeded;

    public LoginViewModel()
    {
        LoginCommand    = new RelayCommand(DoLogin, _ => !string.IsNullOrWhiteSpace(Login) && !IsLoading);
        DevResetCommand = new RelayCommand(_ => DevReset());
    }

    private void DoLogin(object? param)
    {
        var password = param?.ToString() ?? string.Empty;
        IsLoading = true; ErrorMessage = string.Empty;

        var response = AuthService.Login(Login, password);
        IsLoading = false;

        switch (response.Result)
        {
            case AuthService.LoginResult.Success:
                AttemptsMessage = string.Empty;
                LoginSucceeded?.Invoke();
                break;

            case AuthService.LoginResult.AccountLocked:
                ErrorMessage    = "Учётная запись заблокирована. Обратитесь к администратору.";
                AttemptsMessage = string.Empty;
                break;

            default:
                ErrorMessage    = "Неверный логин или пароль.";
                AttemptsMessage = response.AttemptsLeft > 0
                    ? $"Осталось попыток: {response.AttemptsLeft}"
                    : string.Empty;
                break;
        }
    }

    private void DevReset()
    {
        AuthService.DevResetPasswords();
        ErrorMessage    = string.Empty;
        AttemptsMessage = string.Empty;
        System.Windows.MessageBox.Show(
            "✅ Готово!\n\nadmin / admin123\noperator / oper123\nobserver / obs123\n\nВсе блокировки сняты.",
            "DEV: Сброс", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
