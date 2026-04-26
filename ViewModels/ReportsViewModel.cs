using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ClosedXML.Excel;
using EquipmentDB.Data;
using EquipmentDB.Helpers;
using EquipmentDB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace EquipmentDB.ViewModels;

public record ReportItem(int Id, string Name);

public class ChartBar
{
    public string Label    { get; init; } = "";
    public int    Value    { get; init; }
    public double BarWidth { get; init; }
    public Brush  Color    { get; init; } = Brushes.SteelBlue;
}

public class ReportsViewModel : BaseViewModel
{
    private int _selectedReport;
    private ObservableCollection<object> _reportData = [];
    private string _reportTitle = string.Empty;
    private bool _isChartVisible;
    private List<ChartBar> _chartBars = [];

    public int    SelectedReport  { get => _selectedReport;  set { Set(ref _selectedReport, value);  _ = LoadReportAsync(); } }
    public ObservableCollection<object> ReportData { get => _reportData; set => Set(ref _reportData, value); }
    public string ReportTitle     { get => _reportTitle;     set => Set(ref _reportTitle, value); }
    public bool   IsChartVisible  { get => _isChartVisible;  set => Set(ref _isChartVisible, value); }
    public List<ChartBar> ChartBars { get => _chartBars;     set => Set(ref _chartBars, value); }

    public List<ReportItem> Reports { get; } =
    [
        new(0, "Инвентарная ведомость по классам"),
        new(1, "Сводка по состоянию оборудования"),
        new(2, "Аналитика по типам оборудования"),
        new(3, "Журнал событий за 7 дней"),
    ];

    public ICommand RefreshCommand { get; }
    public ICommand ExportCommand  { get; }

    public ReportsViewModel()
    {
        RefreshCommand = new RelayCommand(_ => _ = LoadReportAsync());
        ExportCommand  = new RelayCommand(_ => ExportToExcel());
        _ = LoadReportAsync();
    }

    public async Task LoadReportAsync()
    {
        await using var ctx = new AppDbContext();
        switch (SelectedReport)
        {
            case 0:
                IsChartVisible = false;
                ReportTitle = "Инвентарная ведомость по классам";
                var inv = await ctx.Equipment
                    .Include(e => e.Room).Include(e => e.Type).Include(e => e.Status)
                    .OrderBy(e => e.Room.Cabinet).ThenBy(e => e.InventoryNumber)
                    .Select(e => new { Класс = e.Room.Cabinet + " " + e.Room.Name, Инв_номер = e.InventoryNumber, Название = e.Name, Тип = e.Type.Name, Состояние = e.Status.Name, Дата = e.ArrivalDate.ToString() })
                    .ToListAsync();
                ReportData = new ObservableCollection<object>(inv.Cast<object>());
                break;

            case 1:
                IsChartVisible = false;
                ReportTitle = "Сводка по состоянию оборудования";
                var sum = await ctx.Equipment.GroupBy(e => e.Status.Name)
                    .Select(g => new { Состояние = g.Key, Количество = g.Count() }).ToListAsync();
                ReportData = new ObservableCollection<object>(sum.Cast<object>());
                break;

            case 2:
                ReportTitle = "Аналитика по типам оборудования";
                var typ = await ctx.Equipment.GroupBy(e => e.Type.Name)
                    .Select(g => new { Тип = g.Key, Всего = g.Count(),
                        Исправно  = g.Count(e => e.Status.Name == "Исправно"),
                        В_ремонте = g.Count(e => e.Status.Name == "В ремонте"),
                        Списано   = g.Count(e => e.Status.Name == "Списано") })
                    .OrderByDescending(g => g.Всего).ToListAsync();
                ReportData = new ObservableCollection<object>(typ.Cast<object>());
                // Строим диаграмму
                int maxVal = typ.Count > 0 ? typ.Max(t => t.Всего) : 1;
                ChartBars = typ.Select((t, i) => new ChartBar
                {
                    Label    = t.Тип,
                    Value    = t.Всего,
                    BarWidth = maxVal > 0 ? (t.Всего / (double)maxVal) * 300 : 0,
                    Color    = i % 3 == 0 ? new SolidColorBrush(Color.FromRgb(30, 90, 160))
                             : i % 3 == 1 ? new SolidColorBrush(Color.FromRgb(46, 160, 100))
                             :              new SolidColorBrush(Color.FromRgb(220, 120, 40))
                }).ToList();
                IsChartVisible = ChartBars.Count > 0;
                break;

            case 3:
                IsChartVisible = false;
                ReportTitle = "Журнал событий за последние 7 дней";
                var from = DateTime.Today.AddDays(-7);
                var log = await ctx.EventLog.Include(e => e.User)
                    .Where(e => e.EventTime >= from).OrderByDescending(e => e.EventTime)
                    .Select(e => new { Время = e.EventTime.ToString("dd.MM.yyyy HH:mm"), Пользователь = e.User != null ? e.User.Login : "—", ПК = e.User != null ? "" : "—", Событие = e.EventType, Описание = e.Description })
                    .ToListAsync();
                ReportData = new ObservableCollection<object>(log.Cast<object>());
                break;
        }
    }

    private void ExportToExcel()
    {
        if (ReportData.Count == 0)
        { MessageBox.Show("Нет данных для экспорта.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        var dlg = new SaveFileDialog
        {
            Filter   = "Excel|*.xlsx",
            FileName = $"{ReportTitle.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet(ReportTitle.Length > 31 ? ReportTitle[..31] : ReportTitle);

            // Заголовок
            ws.Cell(1, 1).Value = ReportTitle;
            ws.Cell(1, 1).Style.Font.Bold     = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A5F");

            ws.Cell(2, 1).Value = $"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}";
            ws.Cell(2, 1).Style.Font.Italic    = true;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

            // Получаем свойства через рефлексию
            var first = ReportData[0];
            var props = first.GetType().GetProperties();

            // Шапка таблицы
            for (int i = 0; i < props.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = props[i].Name.Replace("_", " ");
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Данные
            for (int r = 0; r < ReportData.Count; r++)
            {
                var item = ReportData[r];
                for (int c = 0; c < props.Length; c++)
                {
                    var cell = ws.Cell(r + 5, c + 1);
                    cell.Value = props[c].GetValue(item)?.ToString() ?? "";
                    if (r % 2 == 1)
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F5FF");
                }
            }

            ws.Range(ws.Cell(4, 1), ws.Cell(ReportData.Count + 4, props.Length))
              .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(ws.Cell(4, 1), ws.Cell(ReportData.Count + 4, props.Length))
              .Style.Border.InsideBorder  = XLBorderStyleValues.Hair;

            ws.Columns().AdjustToContents();
            // Объединяем ячейки заголовка по всей ширине
            ws.Range(ws.Cell(1, 1), ws.Cell(1, props.Length)).Merge();
            ws.Range(ws.Cell(2, 1), ws.Cell(2, props.Length)).Merge();

            wb.SaveAs(dlg.FileName);

            // Открыть файл сразу
            var result = MessageBox.Show("Экспорт завершён. Открыть файл?", "Готово",
                MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });

            Services.LogService.Log("Экспорт Excel", $"Экспортирован отчёт: {ReportTitle} ({ReportData.Count} строк)");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка экспорта:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
