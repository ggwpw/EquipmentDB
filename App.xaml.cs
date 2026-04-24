using System.Windows;
using System.Windows.Threading;
using EquipmentDB.Views;

namespace EquipmentDB;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show($"Ошибка:\n{ex.Exception.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };
        new LoginWindow().Show();
    }
}
