using System.Collections.ObjectModel;
using System.Windows.Input;
using EquipmentDB.Data;
using EquipmentDB.Helpers;
using EquipmentDB.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipmentDB.ViewModels;

public record ReportItem(int Id, string Name);

public class ReportsViewModel : BaseViewModel
{
    private int _selectedReport;
    private ObservableCollection<object> _reportData = [];
    private string _reportTitle = string.Empty;

    public int    SelectedReport { get => _selectedReport; set { Set(ref _selectedReport, value); _ = LoadReportAsync(); } }
    public ObservableCollection<object> ReportData { get => _reportData; set => Set(ref _reportData, value); }
    public string ReportTitle   { get => _reportTitle;   set => Set(ref _reportTitle, value); }

    public List<ReportItem> Reports { get; } =
    [
        new(0, "Инвентарная ведомость по классам"),
        new(1, "Сводка по состоянию оборудования"),
        new(2, "Аналитика по типам оборудования"),
        new(3, "Журнал событий за 7 дней"),
    ];

    public ICommand RefreshCommand { get; }

    public ReportsViewModel()
    {
        RefreshCommand = new RelayCommand(_ => _ = LoadReportAsync());
        _ = LoadReportAsync();
    }

    public async Task LoadReportAsync()
    {
        await using var ctx = new AppDbContext();
        switch (SelectedReport)
        {
            case 0:
                ReportTitle = "Инвентарная ведомость по классам";
                var inv = await ctx.Equipment.Include(e => e.Room).Include(e => e.Type).Include(e => e.Status)
                    .OrderBy(e => e.Room.Cabinet).ThenBy(e => e.InventoryNumber)
                    .Select(e => new { Класс = e.Room.Cabinet + " " + e.Room.Name, Инв_номер = e.InventoryNumber, Название = e.Name, Тип = e.Type.Name, Состояние = e.Status.Name, Дата = e.ArrivalDate.ToString() })
                    .ToListAsync();
                ReportData = new ObservableCollection<object>(inv.Cast<object>());
                break;
            case 1:
                ReportTitle = "Сводка по состоянию оборудования";
                var sum = await ctx.Equipment.GroupBy(e => e.Status.Name)
                    .Select(g => new { Состояние = g.Key, Количество = g.Count() }).ToListAsync();
                ReportData = new ObservableCollection<object>(sum.Cast<object>());
                break;
            case 2:
                ReportTitle = "Аналитика по типам оборудования";
                var typ = await ctx.Equipment.GroupBy(e => e.Type.Name)
                    .Select(g => new { Тип = g.Key, Всего = g.Count(), Исправно = g.Count(e => e.Status.Name == "Исправно"), В_ремонте = g.Count(e => e.Status.Name == "В ремонте"), Списано = g.Count(e => e.Status.Name == "Списано") })
                    .OrderByDescending(g => g.Всего).ToListAsync();
                ReportData = new ObservableCollection<object>(typ.Cast<object>());
                break;
            case 3:
                ReportTitle = "Журнал событий за последние 7 дней";
                var from = DateTime.Today.AddDays(-7);
                var log = await ctx.EventLog.Include(e => e.User).Where(e => e.EventTime >= from)
                    .OrderByDescending(e => e.EventTime)
                    .Select(e => new { Время = e.EventTime.ToString("dd.MM.yyyy HH:mm"), Пользователь = e.User != null ? e.User.Login : "—", Событие = e.EventType, Описание = e.Description })
                    .ToListAsync();
                ReportData = new ObservableCollection<object>(log.Cast<object>());
                break;
        }
    }
}
