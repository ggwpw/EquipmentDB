using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Input;
using EquipmentDB.Data;
using EquipmentDB.Helpers;
using EquipmentDB.Services;
using Microsoft.EntityFrameworkCore;

namespace EquipmentDB.ViewModels;

/// <summary>
/// Admin-only raw table viewer/editor.
/// Loads any table into a DataTable → DataGrid (AutoGenerateColumns).
/// Changes are pushed back via EF Core raw SQL on Save.
/// </summary>
public class DatabaseEditorViewModel : BaseViewModel
{
    // ── Public state ─────────────────────────────────────────────────────────
    private string _selectedTable = string.Empty;
    private DataView? _tableData;
    private bool _isDirty;
    private bool _isBusy;

    public string  SelectedTable { get => _selectedTable; set { Set(ref _selectedTable, value); _ = LoadTableAsync(); } }
    public DataView? TableData   { get => _tableData;   set => Set(ref _tableData, value); }
    public bool   IsDirty        { get => _isDirty;     set => Set(ref _isDirty, value); }
    public bool   IsBusy         { get => _isBusy;      set => Set(ref _isBusy, value); }

    public ObservableCollection<TableMeta> Tables { get; } =
    [
        new("rooms",           "Кабинеты"),
        new("equipment_types", "Типы оборудования"),
        new("statuses",        "Статусы"),
        new("staff",           "Сотрудники"),
        new("users",           "Пользователи"),
        new("equipment",       "Оборудование"),
        new("event_log",       "Журнал событий"),
        new("login_history",   "История входов"),
    ];

    public ICommand SaveCommand    { get; }
    public ICommand RefreshCommand { get; }
    public ICommand DiscardCommand { get; }

    public DatabaseEditorViewModel()
    {
        SaveCommand    = new RelayCommand(_ => _ = SaveAsync(),   _ => IsDirty && !IsBusy);
        RefreshCommand = new RelayCommand(_ => _ = LoadTableAsync(), _ => !string.IsNullOrEmpty(SelectedTable) && !IsBusy);
        DiscardCommand = new RelayCommand(_ => _ = LoadTableAsync(), _ => IsDirty && !IsBusy);
    }

    // ── Load ─────────────────────────────────────────────────────────────────
    public async Task LoadTableAsync()
    {
        if (string.IsNullOrEmpty(SelectedTable)) { TableData = null; return; }
        IsBusy = true;
        IsDirty = false;
        try
        {
            var dt = await Task.Run(() =>
            {
                using var ctx = new AppDbContext();
                var conn = ctx.Database.GetDbConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT * FROM `{SelectedTable}` ORDER BY id LIMIT 500";
                var da = new System.Data.Common.DbDataAdapter() as System.Data.IDbDataAdapter;
                // Use DataReader to fill DataTable manually
                using var reader = cmd.ExecuteReader();
                var table = new DataTable(SelectedTable);
                table.Load(reader);
                return table;
            });
            var view = dt.DefaultView;
            view.ListChanged += (_, _) => IsDirty = true;
            TableData = view;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки таблицы:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    // ── Save ──────────────────────────────────────────────────────────────────
    public async Task SaveAsync()
    {
        if (TableData == null || string.IsNullOrEmpty(SelectedTable)) return;
        IsBusy = true;
        try
        {
            var dt = TableData.Table!;
            // Only modified rows
            var changed = dt.GetChanges();
            if (changed == null || changed.Rows.Count == 0) { IsDirty = false; return; }

            int saved = 0;
            await Task.Run(() =>
            {
                using var ctx = new AppDbContext();
                var conn = ctx.Database.GetDbConnection();
                conn.Open();

                foreach (DataRow row in changed.Rows)
                {
                    if (row.RowState == DataRowState.Modified)
                    {
                        var setClauses = new List<string>();
                        var id = row["id"];
                        foreach (DataColumn col in dt.Columns)
                        {
                            if (col.ColumnName == "id") continue;
                            setClauses.Add($"`{col.ColumnName}` = '{EscapeSql(row[col.ColumnName])}'");
                        }
                        if (setClauses.Count == 0) continue;
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = $"UPDATE `{SelectedTable}` SET {string.Join(", ", setClauses)} WHERE id = {id}";
                        cmd.ExecuteNonQuery();
                        saved++;
                    }
                    else if (row.RowState == DataRowState.Deleted)
                    {
                        var id = row["id", DataRowVersion.Original];
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = $"DELETE FROM `{SelectedTable}` WHERE id = {id}";
                        try { cmd.ExecuteNonQuery(); saved++; }
                        catch { /* FK constraint — skip */ }
                    }
                }
                dt.AcceptChanges();
            });

            LogService.Log("Редактор БД", $"Сохранено {saved} строк в таблице {SelectedTable}", SelectedTable);
            IsDirty = false;
            MessageBox.Show($"Сохранено {saved} строк.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    private static string EscapeSql(object? value)
        => value == null || value == DBNull.Value ? "NULL" : value.ToString()!.Replace("'", "''");
}

public record TableMeta(string SqlName, string DisplayName)
{
    public override string ToString() => DisplayName;
}
