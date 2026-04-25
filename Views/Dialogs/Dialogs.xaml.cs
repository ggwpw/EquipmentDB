using System.Windows.Media.Imaging;
using System.Windows;
using EquipmentDB.Data;
using EquipmentDB.Models;
using EquipmentDB.Services;
using Microsoft.Win32;

namespace EquipmentDB.Views.Dialogs;

// EquipmentDialog VM
public class EquipmentDialogVm : ViewModels.BaseViewModel
{
    public int Id { get; set; }
    private string _inv="", _name="";
    private int _typeId, _roomId, _statusId;
    private DateTime _arrivalDt = DateTime.Today;
    private string? _specs, _photoPath;

    public string InventoryNumber { get => _inv;       set => Set(ref _inv, value); }
    public string Name            { get => _name;      set => Set(ref _name, value); }
    public int    TypeId          { get => _typeId;    set => Set(ref _typeId, value); }
    public int    RoomId          { get => _roomId;    set => Set(ref _roomId, value); }
    public int    StatusId        { get => _statusId;  set => Set(ref _statusId, value); }
    public DateTime ArrivalDateDt { get => _arrivalDt; set => Set(ref _arrivalDt, value); }
    public string? Specs          { get => _specs;     set => Set(ref _specs, value); }

    // PhotoPath — хранит относительный путь "Photos\ИНВ-00001.jpg"
    public string? PhotoPath
    {
        get => _photoPath;
        set { Set(ref _photoPath, value); OnPropertyChanged(nameof(PhotoFileName)); OnPropertyChanged(nameof(PhotoAbsPath)); }
    }
    // Только имя файла для отображения
    public string PhotoFileName => System.IO.Path.GetFileName(_photoPath) ?? "Фото не выбрано";
    // Абсолютный путь для Image.Source
    public string? PhotoAbsPath => EquipmentDB.Services.PhotoService.ResolveAbsolutePath(_photoPath);

    public List<EquipmentType> Types    { get; set; } = [];
    public List<Room>          Rooms    { get; set; } = [];
    public List<Status>        Statuses { get; set; } = [];
}

public partial class EquipmentDialog : Window
{
    private readonly EquipmentDialogVm _vm = new();
    private readonly int? _editId;

    public EquipmentDialog(Equipment? item)
    {
        InitializeComponent();
        using var ctx = new AppDbContext();
        _vm.Types    = ctx.EquipmentTypes.OrderBy(t => t.Name).ToList();
        _vm.Rooms    = ctx.Rooms.OrderBy(r => r.Cabinet).ToList();
        _vm.Statuses = ctx.Statuses.ToList();
        if (item != null)
        {
            _editId = item.Id; _vm.Id = item.Id;
            _vm.InventoryNumber = item.InventoryNumber; _vm.Name = item.Name;
            _vm.TypeId = item.TypeId; _vm.RoomId = item.RoomId; _vm.StatusId = item.StatusId;
            _vm.ArrivalDateDt = item.ArrivalDate.ToDateTime(TimeOnly.MinValue);
            _vm.Specs = item.Specs; _vm.PhotoPath = item.PhotoPath;
        }
        DataContext = _vm;
        Loaded += (_, _) => UpdatePhotoPreview();
    }

    private void UpdatePhotoPreview()
    {
        // PhotoPreview может быть null если вкладка ещё не отрисована — откладываем
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            if (PhotoPreview == null) return;
            var abs = _vm.PhotoAbsPath;
            if (abs != null && System.IO.File.Exists(abs))
            {
                try
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(abs, UriKind.Absolute);
                    bmp.EndInit();
                    PhotoPreview.Source = bmp;
                }
                catch { PhotoPreview.Source = null; }
            }
            else
            {
                PhotoPreview.Source = null;
            }
        });
    }

    private void OnSave(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_vm.InventoryNumber) || string.IsNullOrWhiteSpace(_vm.Name) ||
            _vm.TypeId == 0 || _vm.RoomId == 0 || _vm.StatusId == 0)
        { MessageBox.Show("Заполните все обязательные поля (*).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        using var ctx = new AppDbContext();
        if (_editId.HasValue)
        {
            var eq = ctx.Equipment.Find(_editId.Value); if (eq == null) return;
            var old = $"{eq.InventoryNumber} — {eq.Name}";
            eq.InventoryNumber = _vm.InventoryNumber; eq.Name = _vm.Name;
            eq.TypeId = _vm.TypeId; eq.RoomId = _vm.RoomId; eq.StatusId = _vm.StatusId;
            eq.ArrivalDate = DateOnly.FromDateTime(_vm.ArrivalDateDt);
            eq.Specs = _vm.Specs; eq.PhotoPath = _vm.PhotoPath;
            ctx.SaveChanges();
            LogService.Log("Изменение", $"Изменено оборудование: {old} -> {eq.Name}", "equipment", eq.Id);
        }
        else
        {
            var eq = new Equipment { InventoryNumber = _vm.InventoryNumber, Name = _vm.Name,
                TypeId = _vm.TypeId, RoomId = _vm.RoomId, StatusId = _vm.StatusId,
                ArrivalDate = DateOnly.FromDateTime(_vm.ArrivalDateDt), Specs = _vm.Specs, PhotoPath = _vm.PhotoPath };
            ctx.Equipment.Add(eq); ctx.SaveChanges();
            LogService.Log("Добавление", $"Добавлено оборудование: {eq.InventoryNumber} — {eq.Name}", "equipment", eq.Id);
        }
        DialogResult = true;
    }

    private void OnCancel(object s, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_vm.InventoryNumber) || !string.IsNullOrWhiteSpace(_vm.Name))
        {
            if (MessageBox.Show("Есть несохранённые данные. Закрыть?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        }
        DialogResult = false;
    }

    private void OnBrowsePhoto(object s, RoutedEventArgs e)
    {
        var d = new OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif" };
        if (d.ShowDialog() != true) return;

        // Копируем в папку Photos рядом с exe, имя файла = инв.номер
        try
        {
            var invNum = _vm.InventoryNumber.Trim();
            if (string.IsNullOrWhiteSpace(invNum))
            { MessageBox.Show("Сначала введите инвентарный номер.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            _vm.PhotoPath = PhotoService.CopyPhoto(d.FileName, invNum);
            UpdatePhotoPreview();
        }
        catch (Exception ex)
        {
            // Если копирование не удалось — сохраняем абсолютный путь
            _vm.PhotoPath = d.FileName;
            UpdatePhotoPreview();
            MessageBox.Show($"Не удалось скопировать файл:\n{ex.Message}\n\nПуть сохранён как абсолютный.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnRemovePhoto(object s, RoutedEventArgs e)
    {
        _vm.PhotoPath = null;
        PhotoPreview.Source = null;
    }
}

public partial class RoomDialog : Window
{
    private readonly Room _room;
    private readonly bool _isNew;
    public RoomDialog(Room? room)
    {
        InitializeComponent();
        _isNew = room == null;
        _room = room != null
            ? new Room { Id=room.Id, Cabinet=room.Cabinet, Name=room.Name, Capacity=room.Capacity, PhotoPath=room.PhotoPath }
            : new Room();
        DataContext = _room;
        Loaded += (_, _) => UpdateRoomPhotoPreview();
    }

    private void UpdateRoomPhotoPreview()
    {
        var abs = PhotoService.ResolveAbsolutePath(_room.PhotoPath);
        if (abs != null && System.IO.File.Exists(abs))
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(abs, UriKind.Absolute);
            bmp.EndInit();
            RoomPhotoPreview.Source = bmp;
        }
        else RoomPhotoPreview.Source = null;
    }

    private void OnBrowsePhoto(object s, RoutedEventArgs e)
    {
        var d = new Microsoft.Win32.OpenFileDialog
            { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*" };
        if (d.ShowDialog() != true) return;
        var key = string.IsNullOrWhiteSpace(_room.Cabinet) ? $"room_{_room.Id}" : _room.Cabinet;
        _room.PhotoPath = PhotoService.CopyPhoto(d.FileName, "room_" + key);
        // Room — plain POCO, нет INotifyPropertyChanged. Обновляем UI напрямую.
        RoomPhotoPreview.Visibility = System.Windows.Visibility.Visible;
        UpdateRoomPhotoPreview();
    }

    private void OnRemovePhoto(object s, RoutedEventArgs e)
    {
        PhotoService.DeletePhoto(_room.PhotoPath);
        _room.PhotoPath = null;
        RoomPhotoPreview.Source = null;
    }

    private void OnSave(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_room.Cabinet) || string.IsNullOrWhiteSpace(_room.Name))
        { MessageBox.Show("Заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (int.TryParse(CapacityBox.Text, out int cap)) _room.Capacity = cap;
        using var ctx = new AppDbContext();
        if (_isNew)
        {
            ctx.Rooms.Add(_room); ctx.SaveChanges();
            LogService.Log("Добавление", $"Добавлен класс {_room.Cabinet}", "rooms", _room.Id);
        }
        else
        {
            var r = ctx.Rooms.Find(_room.Id); if (r == null) return;
            r.Cabinet=_room.Cabinet; r.Name=_room.Name; r.Capacity=_room.Capacity; r.PhotoPath=_room.PhotoPath;
            ctx.SaveChanges();
            LogService.Log("Изменение", $"Изменён класс {r.Cabinet}", "rooms", r.Id);
        }
        DialogResult = true;
    }
    private void OnCancel(object s, RoutedEventArgs e) => DialogResult = false;
}

public partial class StaffDialog : Window
{
    private readonly Staff _staff;
    private readonly bool _isNew;
    public StaffDialog(Staff? staff, Room? defaultRoom)
    {
        InitializeComponent();
        _isNew = staff == null;
        _staff = staff != null ? new Staff { Id=staff.Id, FullName=staff.FullName, Position=staff.Position, Phone=staff.Phone, Email=staff.Email, RoomId=staff.RoomId } : new Staff { RoomId=defaultRoom?.Id };
        DataContext = _staff;
        using var ctx = new AppDbContext();
        RoomCombo.ItemsSource   = ctx.Rooms.OrderBy(r => r.Cabinet).ToList();
        RoomCombo.SelectedValue = _staff.RoomId;
    }
    private void OnSave(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_staff.FullName))
        { MessageBox.Show("Введите ФИО.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        using var ctx = new AppDbContext();
        if (_isNew) { ctx.Staff.Add(_staff); ctx.SaveChanges(); LogService.Log("Добавление", $"Добавлен сотрудник {_staff.FullName}", "staff", _staff.Id); }
        else { var st = ctx.Staff.Find(_staff.Id); if (st==null) return; st.FullName=_staff.FullName; st.Position=_staff.Position; st.Phone=_staff.Phone; st.Email=_staff.Email; st.RoomId=_staff.RoomId; ctx.SaveChanges(); LogService.Log("Изменение", $"Изменён сотрудник {st.FullName}", "staff", st.Id); }
        DialogResult = true;
    }
    private void OnCancel(object s, RoutedEventArgs e) => DialogResult = false;
}

public partial class UserDialog : Window
{
    private readonly bool _isNew;
    private readonly int? _editId;
    public string Login   { get; set; } = "";
    public string Role    { get; set; } = "observer";
    public int?   StaffId { get; set; }
    public List<Staff>  AllStaff { get; set; } = [];
    public string[] Roles { get; } = ["admin", "operator", "observer"];

    public UserDialog(User? user)
    {
        InitializeComponent();
        _isNew = user == null; _editId = user?.Id;
        if (user != null) { Login=user.Login; Role=user.Role; StaffId=user.StaffId; }
        using var ctx = new AppDbContext();
        AllStaff = ctx.Staff.OrderBy(s => s.FullName).ToList();
        DataContext = this;
    }
    private void OnSave(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Login))
        { MessageBox.Show("Введите логин.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        using var ctx = new AppDbContext();
        if (_isNew)
        {
            var pwd = PwdBox.Password;
            if (string.IsNullOrWhiteSpace(pwd)) { MessageBox.Show("Введите пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var u = new User { Login=Login, PasswordHash=BCrypt.Net.BCrypt.HashPassword(pwd), Role=Role, StaffId=StaffId };
            ctx.Users.Add(u); ctx.SaveChanges();
            LogService.Log("Добавление", $"Создан пользователь {u.Login}", "users", u.Id);
        }
        else
        {
            var u = ctx.Users.Find(_editId!.Value); if (u==null) return;
            u.Login=Login; u.Role=Role; u.StaffId=StaffId; ctx.SaveChanges();
            LogService.Log("Изменение", $"Изменён пользователь {u.Login}", "users", u.Id);
        }
        DialogResult = true;
    }
    private void OnCancel(object s, RoutedEventArgs e) => DialogResult = false;
}

public partial class ResetPasswordDialog : Window
{
    public string NewPassword { get; private set; } = "";
    public ResetPasswordDialog(string login) { InitializeComponent(); Title = $"Сброс пароля — {login}"; }
    private void OnSave(object s, RoutedEventArgs e)
    {
        if (PwdBox.Password != ConfirmBox.Password) { MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (PwdBox.Password.Length < 4) { MessageBox.Show("Минимум 4 символа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        NewPassword = PwdBox.Password;
        DialogResult = true;
    }
    private void OnCancel(object s, RoutedEventArgs e) => DialogResult = false;
}
