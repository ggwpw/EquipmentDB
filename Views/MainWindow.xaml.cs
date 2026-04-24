using System.Windows;
using EquipmentDB.ViewModels;

namespace EquipmentDB.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ((MainViewModel)DataContext).LogoutRequested += () => { new LoginWindow().Show(); Close(); };
    }
}