using System.Windows;
using System.Windows.Input;
using EquipmentDB.ViewModels;

namespace EquipmentDB.Views;

public partial class LoginWindow : Window
{
    private LoginViewModel Vm => (LoginViewModel)DataContext;

    public LoginWindow()
    {
        InitializeComponent();
        Vm.LoginSucceeded      += OnLoginSucceeded;
        Vm.AppCloseRequested   += OnAppCloseRequested;
    }

    private void OnLoginSucceeded()
    {
        new MainWindow().Show();
        Close();
    }

    private void OnAppCloseRequested()
    {
        MessageBox.Show("Превышено количество попыток входа.\nПриложение будет закрыто.",
            "Доступ закрыт", MessageBoxButton.OK, MessageBoxImage.Error);
        System.Windows.Application.Current.Shutdown();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        // Enter в поле логина → переход на пароль
        if (sender == LoginBox)
        {
            PwdBox.Focus();
            e.Handled = true;
            return;
        }
        // Enter в поле пароля → войти
        Vm.LoginCommand.Execute(PwdBox.Password);
    }

    private void OnLoginClick(object sender, RoutedEventArgs e)
    {
        Vm.LoginCommand.Execute(PwdBox.Password); // <-- .Password, не PwdBox
    }
}
