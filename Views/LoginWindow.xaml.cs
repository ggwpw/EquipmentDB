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
        Vm.LoginSucceeded += OnLoginSucceeded;
    }

    private void OnLoginSucceeded()
    {
        new MainWindow().Show();
        Close();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Vm.LoginCommand.Execute(PwdBox.Password); // <-- .Password, не PwdBox
    }

    private void OnLoginClick(object sender, RoutedEventArgs e)
    {
        Vm.LoginCommand.Execute(PwdBox.Password); // <-- .Password, не PwdBox
    }
}
