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
        await using var ctx = new AppDbContext();
        var toEnd = To.Date.AddDays(1);
        var q = ctx.EventLog.Include(e => e.User)
            .Where(e => e.EventTime >= From && e.EventTime < toEnd).AsQueryable();
        if (!string.IsNullOrWhiteSpace(SearchUser))
            q = q.Where(e => e.User != null && e.User.Login.Contains(SearchUser));
        Entries = new ObservableCollection<EventLogEntry>(
            await q.OrderByDescending(e => e.EventTime).ToListAsync());
    }
}
