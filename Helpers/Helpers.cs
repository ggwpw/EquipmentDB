using System.Windows.Input;
using EquipmentDB.Models;

namespace EquipmentDB.Helpers;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    { _execute = execute; _canExecute = canExecute; }
    public bool CanExecute(object? p) => _canExecute?.Invoke(p) ?? true;
    public void Execute(object? p) => _execute(p);
}

public static class CurrentSession
{
    public static User? User { get; set; }
    public static bool IsAdmin => User?.Role == "admin";
    public static bool IsOperator => User?.Role == "operator";
    public static bool CanEdit => IsAdmin || IsOperator;
    public static void Clear() => User = null;
}
