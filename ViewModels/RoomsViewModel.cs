using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using EquipmentDB.Data;
using EquipmentDB.Helpers;
using Microsoft.EntityFrameworkCore;
using EquipmentDB.Models;
using EquipmentDB.Services;

namespace EquipmentDB.ViewModels;

public class RoomsViewModel : BaseViewModel
{
    private ObservableCollection<Room> _rooms = [];
    private ObservableCollection<Staff> _staff = [];
    private Room? _selectedRoom;
    private Staff? _selectedStaff;

    public ObservableCollection<Room>  Rooms         { get => _rooms;        set => Set(ref _rooms, value); }
    public ObservableCollection<Staff> Staff         { get => _staff;        set => Set(ref _staff, value); }
    public Room?  SelectedRoom  { get => _selectedRoom;  set { Set(ref _selectedRoom, value);  LoadStaff(); } }
    public Staff? SelectedStaff { get => _selectedStaff; set => Set(ref _selectedStaff, value); }
    public bool CanEdit => CurrentSession.CanEdit;

    public ICommand AddRoomCommand    { get; }
    public ICommand EditRoomCommand   { get; }
    public ICommand DeleteRoomCommand { get; }
    public ICommand AddStaffCommand   { get; }
    public ICommand EditStaffCommand  { get; }
    public ICommand DeleteStaffCommand{ get; }

    public RoomsViewModel()
    {
        AddRoomCommand     = new RelayCommand(_ => OpenRoomDialog(null),          _ => CanEdit);
        EditRoomCommand    = new RelayCommand(_ => OpenRoomDialog(SelectedRoom),  _ => SelectedRoom != null && CanEdit);
        DeleteRoomCommand  = new RelayCommand(_ => DeleteRoom(),                  _ => SelectedRoom != null && CanEdit);
        AddStaffCommand    = new RelayCommand(_ => OpenStaffDialog(null),         _ => CanEdit);
        EditStaffCommand   = new RelayCommand(_ => OpenStaffDialog(SelectedStaff),_ => SelectedStaff != null && CanEdit);
        DeleteStaffCommand = new RelayCommand(_ => DeleteStaff(),                 _ => SelectedStaff != null && CanEdit);
        LoadRooms();
    }

    public void LoadRooms()
    {
        try
        {
            using var ctx = new AppDbContext();
            Rooms = new ObservableCollection<Room>(ctx.Rooms.OrderBy(r => r.Cabinet).ToList());
        }
        catch (Exception ex) when (ex.Message.Contains("photo_path") || ex.Message.Contains("Unknown column"))
        {
            // photo_path ещё не добавлена в БД — загружаем без неё
            using var ctx = new AppDbContext();
            var conn = ctx.Database.GetDbConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, cabinet, name, capacity FROM rooms ORDER BY cabinet";
            using var reader = cmd.ExecuteReader();
            var list = new List<Room>();
            while (reader.Read())
                list.Add(new Room
                {
                    Id       = reader.GetInt32(0),
                    Cabinet  = reader.GetString(1),
                    Name     = reader.GetString(2),
                    Capacity = reader.GetInt32(3)
                });
            Rooms = new ObservableCollection<Room>(list);
        }
    }

    private void LoadStaff()
    {
        if (SelectedRoom == null) { Staff = []; return; }
        using var ctx = new AppDbContext();
        Staff = new ObservableCollection<Staff>(
            ctx.Staff.Where(s => s.RoomId == SelectedRoom.Id).OrderBy(s => s.FullName).ToList());
    }

    private void OpenRoomDialog(Room? room) { var d = new Views.Dialogs.RoomDialog(room); if (d.ShowDialog() == true) LoadRooms(); }
    private void OpenStaffDialog(Staff? staff) { var d = new Views.Dialogs.StaffDialog(staff, SelectedRoom); if (d.ShowDialog() == true) LoadStaff(); }

    private void DeleteRoom()
    {
        if (SelectedRoom == null) return;
        if (MessageBox.Show($"Удалить класс «{SelectedRoom.Name}»?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        using var ctx = new AppDbContext();
        var r = ctx.Rooms.Find(SelectedRoom.Id); if (r == null) return;
        try { ctx.Rooms.Remove(r); ctx.SaveChanges(); LogService.Log("Удаление", $"Удалён класс {r.Cabinet}", "rooms", r.Id); LoadRooms(); }
        catch { MessageBox.Show("Невозможно удалить — есть связанные записи.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void DeleteStaff()
    {
        if (SelectedStaff == null) return;
        if (MessageBox.Show($"Удалить «{SelectedStaff.FullName}»?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        using var ctx = new AppDbContext();
        var s = ctx.Staff.Find(SelectedStaff.Id); if (s == null) return;
        ctx.Staff.Remove(s); ctx.SaveChanges();
        LogService.Log("Удаление", $"Удалён сотрудник {s.FullName}", "staff", s.Id);
        LoadStaff();
    }
}
