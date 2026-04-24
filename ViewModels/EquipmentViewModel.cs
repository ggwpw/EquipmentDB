using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using EquipmentDB.Data;
using EquipmentDB.Helpers;
using EquipmentDB.Models;
using EquipmentDB.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace EquipmentDB.ViewModels;

public class EquipmentViewModel : BaseViewModel
{
    private ObservableCollection<Equipment> _items = [];
    private Equipment? _selected;
    private string _search = string.Empty;
    private int? _filterRoomId, _filterStatusId;
    private List<Room> _rooms = [];
    private List<Status> _statuses = [];

    public ObservableCollection<Equipment> Items    { get => _items;    set => Set(ref _items, value); }
    public Equipment?                      Selected { get => _selected; set => Set(ref _selected, value); }
    public string Search { get => _search; set { Set(ref _search, value); _ = LoadItemsAsync(); } }
    public int? FilterRoomId
    {
        get => _filterRoomId;
        set { if (_filterRoomId == value) return; Set(ref _filterRoomId, value); _ = LoadItemsAsync(); }
    }
    public int? FilterStatusId
    {
        get => _filterStatusId;
        set { if (_filterStatusId == value) return; Set(ref _filterStatusId, value); _ = LoadItemsAsync(); }
    }
    public List<Room>   Rooms    { get => _rooms;    set => Set(ref _rooms, value); }
    public List<Status> Statuses { get => _statuses; set => Set(ref _statuses, value); }
    public bool CanEdit => CurrentSession.CanEdit;

    public ICommand RefreshCommand      { get; }
    public ICommand AddCommand          { get; }
    public ICommand EditCommand         { get; }
    public ICommand DeleteCommand       { get; }
    public ICommand ExportCommand       { get; }
    public ICommand ClearFiltersCommand { get; }

    public EquipmentViewModel()
    {
        RefreshCommand      = new RelayCommand(_ => _ = LoadAsync());
        AddCommand          = new RelayCommand(_ => OpenDialog(null),     _ => CanEdit);
        EditCommand         = new RelayCommand(_ => OpenDialog(Selected), _ => Selected != null && CanEdit);
        DeleteCommand       = new RelayCommand(_ => DeleteSelected(),     _ => Selected != null && CanEdit);
        ExportCommand       = new RelayCommand(_ => Export());
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
        _ = LoadAsync();
    }

    // Full reload: filter lists + items
    public async Task LoadAsync()
    {
        await using var ctx = new AppDbContext();
        Rooms    = await ctx.Rooms.OrderBy(r => r.Cabinet).ToListAsync();
        Statuses = await ctx.Statuses.ToListAsync();
        await LoadItemsAsync();
    }

    // Items only — called when filters change (avoids ComboBox rebind loop)
    public async Task LoadItemsAsync()
    {
        await using var ctx = new AppDbContext();
        var q = ctx.Equipment.Include(e => e.Type).Include(e => e.Room).Include(e => e.Status).AsQueryable();
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(e => e.Name.Contains(Search) || e.InventoryNumber.Contains(Search));
        if (FilterRoomId.HasValue)   q = q.Where(e => e.RoomId   == FilterRoomId);
        if (FilterStatusId.HasValue) q = q.Where(e => e.StatusId == FilterStatusId);
        Items = new ObservableCollection<Equipment>(await q.OrderBy(e => e.InventoryNumber).ToListAsync());
    }

    private void OpenDialog(Equipment? item)
    {
        var dlg = new Views.Dialogs.EquipmentDialog(item);
        if (dlg.ShowDialog() == true) _ = LoadAsync();
    }

    private void DeleteSelected()
    {
        if (Selected == null) return;
        if (MessageBox.Show($"Удалить «{Selected.Name}»?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        using var ctx = new AppDbContext();
        var eq = ctx.Equipment.Find(Selected.Id);
        if (eq == null) return;
        ctx.Equipment.Remove(eq);
        ctx.SaveChanges();
        LogService.Log("Удаление", $"Удалено: {Selected.InventoryNumber} — {Selected.Name}", "equipment", Selected.Id);
        _ = LoadAsync();
    }

    private void Export()
    {
        var dlg = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"equipment_{DateTime.Now:yyyyMMdd}" };
        if (dlg.ShowDialog() != true) return;
        ExcelService.ExportEquipment([.. Items], dlg.FileName);
        MessageBox.Show("Экспорт завершён.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ClearFilters()
    {
        _filterRoomId = null; _filterStatusId = null; _search = string.Empty;
        OnPropertyChanged(nameof(FilterRoomId)); OnPropertyChanged(nameof(FilterStatusId)); OnPropertyChanged(nameof(Search));
        _ = LoadAsync();
    }
}
