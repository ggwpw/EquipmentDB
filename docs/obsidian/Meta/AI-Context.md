# 🤖 AI Context — читать первым

> Этот файл — сжатый контекст всего проекта.  
> Подай его AI **первым сообщением** перед тем как задавать вопросы.

---

## Проект одной строкой

Десктопное WPF-приложение на .NET 8 для учёта компьютерного оборудования в учебном заведении. БД — MySQL 8. Архитектура — MVVM. ORM — Entity Framework Core + Pomelo.

---

## Технологический стек

```
.NET 8 / WPF
MySQL 8 → EF Core (Pomelo.EntityFrameworkCore.MySql 8.0.2)
UseSnakeCaseNamingConvention() — все поля в snake_case в БД
BCrypt.Net-Next 4.0.3 — хэширование паролей
ClosedXML 0.102.3 — экспорт в Excel
mysqldump — резервное копирование через Process.Start()
```

---

## Структура папок

```
EquipmentDB/
├── Models/          # 7 моделей EF Core
├── Data/            # AppDbContext, DatabaseConfig
├── Helpers/         # RelayCommand, CurrentSession (статик)
├── Converters/      # 9 IValueConverter
├── Services/        # AuthService, LogService, BackupService, ExcelService
├── ViewModels/      # 7 VM наследуют BaseViewModel : INotifyPropertyChanged
├── Resources/       # Styles.xaml (единая тема, все стили глобальные)
├── Views/
│   ├── LoginWindow.xaml/.cs
│   ├── MainWindow.xaml/.cs
│   ├── Pages/       # 5 UserControl-страниц
│   └── Dialogs/     # 5 диалоговых окон (Window)
├── config.json      # строка подключения к БД
└── EquipmentDB.csproj
```

---

## Модели (8 таблиц)

| Класс C# | Таблица MySQL | Ключевые поля |
|---|---|---|
| `Room` | `rooms` | id, cabinet, name, capacity |
| `EquipmentType` | `equipment_types` | id, name, description |
| `Status` | `statuses` | id, name, color_hex |
| `Staff` | `staff` | id, full_name, position, phone, email, room_id |
| `User` | `users` | id, login, password_hash, role, staff_id, is_active, failed_attempts |
| `Equipment` | `equipment` | id, inventory_number, name, type_id, room_id, status_id, arrival_date, specs, photo_path |
| `EventLogEntry` | `event_log` | id, user_id, event_type, description, table_name, record_id, event_time |
| `LoginHistory` | `login_history` | id, user_id, login_time, pc_name, success |

---

## Роли пользователей

| Роль (в БД) | Что может |
|---|---|
| `admin` | Всё: CRUD оборудования, справочники, пользователи, журнал, бэкап |
| `operator` | CRUD оборудования, просмотр справочников, отчёты |
| `observer` | Только просмотр и отчёты |

Проверка роли: `CurrentSession.IsAdmin`, `CurrentSession.CanEdit`  
Видимость кнопок в UI: `Visibility="{Binding CanEdit, Converter={StaticResource BoolToVis}}"`

---

## Авторизация

- Логин/пароль → `AuthService.Login(login, password)`
- Пароль проверяется через `BCrypt.Net.BCrypt.Verify()`
- 3 неудачных попытки → `user.IsActive = false` (блокировка)
- Каждая попытка пишется в `login_history`
- После входа: `CurrentSession.User` заполнен

---

## Навигация (MainViewModel)

ContentControl в MainWindow.xaml показывает текущую страницу через DataTemplate.  
Переключение: `CurrentView = new EquipmentViewModel()` → автоматически рендерится `EquipmentPage`.

---

## Журналирование событий

Везде через статический вызов:
```csharp
LogService.Log("Тип события", "Описание", "таблица", recordId);
```
Пишет в `event_log`. userId берётся из `CurrentSession.User?.Id` автоматически.

---

## Тестовые данные

| Логин | Пароль | Роль |
|---|---|---|
| admin | admin123 | Администратор |
| operator | oper123 | Оператор |
| observer | obs123 | Наблюдатель |

БД: `equipment_db` на `localhost:3306`  
Строка подключения в `config.json` (рядом с .exe)

---

## Важные соглашения

- Все VM наследуют `BaseViewModel`, используют `Set(ref _field, value)`
- Команды — `RelayCommand`, привязка к `CanExecute` через лямбды
- Async-загрузка данных: `async Task LoadAsync()`, вызов `_ = LoadAsync()`
- Диалоги возвращают `DialogResult = true/false`, после `if (dlg.ShowDialog() == true)`
- Стили глобальные в `Styles.xaml`, ключи: `BtnPrimary`, `BtnSecondary`, `BtnDanger`, `BtnNav`, `Card`, `FormLabel`, `PageTitle`
- Конвертеры в `Converters.cs`, ключи в XAML: `BoolToVis`, `HexToBrush`, `RoleConv`, `ActiveConv`, `ActiveBrush`, `NullToVis`

---

## Текущие проблемы

> Смотри [[../Errors/Error-Log]]
