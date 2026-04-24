using System.Windows.Input;
using EquipmentDB.Helpers;
using EquipmentDB.Services;

namespace EquipmentDB.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel? _currentView;
    private string _pageTitle = string.Empty;

    public BaseViewModel? CurrentView  { get => _currentView;  set => Set(ref _currentView, value); }
    public string         PageTitle    { get => _pageTitle;    set => Set(ref _pageTitle, value); }
    public string UserLogin       => CurrentSession.User?.Login ?? "";
    public string UserRoleDisplay => CurrentSession.User?.Role switch
    { "admin" => "Администратор", "operator" => "Оператор", "observer" => "Наблюдатель", _ => "" };
    public bool IsAdmin => CurrentSession.IsAdmin;
    public bool CanEdit => CurrentSession.CanEdit;

    public ICommand NavEquipmentCommand { get; }
    public ICommand NavRoomsCommand     { get; }
    public ICommand NavUsersCommand     { get; }
    public ICommand NavEventLogCommand  { get; }
    public ICommand NavReportsCommand   { get; }
    public ICommand LogoutCommand       { get; }
    public event Action? LogoutRequested;

    public MainViewModel()
    {
        NavEquipmentCommand = new RelayCommand(_ => Go(new EquipmentViewModel(), "Оборудование"));
        NavRoomsCommand     = new RelayCommand(_ => Go(new RoomsViewModel(),     "Классы и сотрудники"));
        NavUsersCommand     = new RelayCommand(_ => Go(new UsersViewModel(),     "Пользователи"), _ => IsAdmin);
        NavEventLogCommand  = new RelayCommand(_ => Go(new EventLogViewModel(),  "Журнал событий"), _ => IsAdmin);
        NavReportsCommand   = new RelayCommand(_ => Go(new ReportsViewModel(),   "Отчёты"));
        LogoutCommand       = new RelayCommand(_ => Logout());
        Go(new EquipmentViewModel(), "Оборудование");
    }

    private void Go(BaseViewModel vm, string title) { CurrentView = vm; PageTitle = title; }

    private void Logout()
    {
        LogService.Log("Выход", $"Пользователь {CurrentSession.User?.Login} вышел из системы");
        CurrentSession.Clear();
        LogoutRequested?.Invoke();
    }
}
