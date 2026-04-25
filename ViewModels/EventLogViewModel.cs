using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using EquipmentDB.Data;
using EquipmentDB.Helpers;
using EquipmentDB.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipmentDB.ViewModels;

public class EventLogViewModel : BaseViewModel
{
    private ObservableCollection<EventLogEntry> _entries = [];
    private DateTime _from = DateTime.Today.AddDays(-7);
    private DateTime _to   = DateTime.Today;
    private string _searchUser = string.Empty;
    private readonly DispatcherTimer _autoRefresh;

    public ObservableCollection<EventLogEntry> Entries    { get => _entries;    set => Set(ref _entries, value); }
    public DateTime From       { get => _from;       set { Set(ref _from, value);       _ = LoadAsync(); } }
    public DateTime To         { get => _to;         set { Set(ref _to, value);         _ = LoadAsync(); } }
    public string   SearchUser { get => _searchUser; set { Set(ref _searchUser, value); _ = LoadAsync(); } }

    public ICommand RefreshCommand { get; }

    public EventLogViewModel()
    {
        RefreshCommand = new RelayCommand(_ => _ = LoadAsync());
        _ = LoadAsync();
        // Auto-refresh every 30 sec for multi-user environments
        _autoRefresh = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _autoRefresh.Tick += (_, _) => _ = LoadAsync();
        _autoRefresh.Start();
    }

    public async Task LoadAsync()
    {
        try
        {
            await using var ctx = new AppDbContext();
            var toEnd = To.Date.AddDays(1);
            var q = ctx.EventLog.Include(e => e.User)
                .Where(e => e.EventTime >= From && e.EventTime < toEnd).AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchUser))
                q = q.Where(e => e.User != null && e.User.Login.Contains(SearchUser));
            Entries = new ObservableCollection<EventLogEntry>(
                await q.OrderByDescending(e => e.EventTime).ToListAsync());
        }
        catch (Exception ex) when (ex.Message.Contains("machine_name") || ex.Message.Contains("Unknown column"))
        {
            // Колонка machine_name ещё не добавлена в БД — загружаем без неё
            await LoadLegacyAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Ошибка загрузки журнала:\n\n{ex.Message}",
                "Журнал событий", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    // Fallback: запрос без колонки machine_name (для БД без миграции)
    private async Task LoadLegacyAsync()
    {
        try
        {
            await using var ctx = new AppDbContext();
            // Явно указываем колонки — machine_name подставляем как NULL
            var entries = await ctx.EventLog
                .FromSqlRaw(
                    "SELECT id, user_id, event_type, description, table_name, " +
                    "record_id, event_time, NULL AS machine_name FROM event_log")
                .Include(e => e.User)
                .Where(e => e.EventTime >= From && e.EventTime < To.Date.AddDays(1))
                .OrderByDescending(e => e.EventTime)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(SearchUser))
                entries = entries.Where(e => e.User?.Login?.Contains(SearchUser) == true).ToList();

            Entries = new ObservableCollection<EventLogEntry>(entries);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Ошибка загрузки журнала:\n\n{ex.Message}\n\n" +
                "Запусти для полного функционала:\ndatabase/migrate_add_machine_name.sql",
                "Журнал событий", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }
}
