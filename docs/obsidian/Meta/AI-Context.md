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

---

## Changelog (последние изменения)

### Сессия 2 — fixes batch 1

**Модели:**
- `EventLogEntry` → добавлено поле `MachineName` (VARCHAR 100)
- `database/init.sql` обновлён; `database/migrate_add_machine_name.sql` — для апдейта существующей БД

**Helpers.cs — новые attached behaviors:**
- `PhoneMask.IsEnabled="True"` на TextBox → маска `+7 (XXX) XXX-XX-XX`
- `DataGridHelper.EnableCopyRow="True"` на DataGrid → ПКМ → «Копировать строку»

**Конвертеры (Converters.cs):**
- `NullToVisibilityInverseConverter` → ключ `NullToVisInv` — показывает элемент когда значение NULL (для placeholder в ComboBox)

**EquipmentViewModel:**
- Разделён на `LoadAsync()` (полная, при старте) и `LoadItemsAsync()` (только Items, при фильтрации)
- Guard `if (_filterRoomId == value) return;` — предотвращает рекурсивный ресет ComboBox

**Views/Dialogs:**
- `StaffDialog.xaml` Height 380→420, телефон использует `helpers:PhoneMask.IsEnabled`
- `RoomDialog.xaml` Height 280→310, CapacityBox → `UpdateSourceTrigger=LostFocus` + failsafe в OnSave

**MainViewModel + MainWindow sidebar:**
- Добавлено свойство `UserFullName` → `User.Staff.FullName ?? User.Login`
- Sidebar показывает: **ФИО** (bold белый) → **Роль • (логин)** (приглушённый синий)
- Новый пункт меню «🗄 Редактор БД» — только для admin

**Новые файлы:**
- `ViewModels/DatabaseEditorViewModel.cs` — admin raw table editor (ComboBox + DataGrid + Save/Discard)
- `Views/Pages/DatabaseEditorPage.xaml` + `.cs`

**UsersViewModel:** блокировка самого себя защищена guard-ом

**EventLogViewModel:** автообновление каждые 30 сек (DispatcherTimer)

**Все DataGrid на страницах:** `helpers:DataGridHelper.EnableCopyRow="True"` → ПКМ копирует строку в буфер

---

## Что передать следующей нейронке

> Скопируй целиком в первое сообщение

```
Репозиторий: https://github.com/ggwpw/EquipmentDB
Токен: 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

Сделай git clone с токеном и прочитай:
  docs/obsidian/Meta/AI-Context.md — главный контекст
  docs/obsidian/Meta/Conventions.md — соглашения по коду
  docs/obsidian/UI/Windows-and-Pages.xaml — описание всех страниц

Стек: WPF / .NET 8 / MySQL 8 / EF Core (Pomelo) / MVVM / BCrypt
Главные файлы: Helpers/Helpers.cs, Converters/Converters.cs,
               Resources/Styles.xaml, Data/AppDbContext.cs,
               ViewModels/MainViewModel.cs, Services/LogService.cs

Текущая задача: [ВСТАВЬ СВОЮ ЗАДАЧУ]
```

---

## Changelog — последняя сессия

### Сессия 5 — финальные фичи по ТЗ

**Новый файл:** `Views/Dialogs/ChangePasswordDialog.xaml` + code-behind в `Dialogs.xaml.cs`
- Форма: текущий пароль + новый + подтверждение (мин. 6 симв.)
- Кнопка «🔑 Сменить пароль» в сайдбаре `MainWindow` для ВСЕХ ролей
- `MainViewModel.ChangePasswordCommand` → открывает диалог

**Отчёты (`ReportsViewModel.cs` + `ReportsPage.xaml`):**
- Отчёт 4 добавлен: «Состояние оборудования по классам» — перекрёстный запрос (п.5.4 ТЗ)
  Колонки: Класс, Всего, Исправно, В_ремонте, Списано, Процент_неисправных
- `CalcFaultPercent(total, faulty)` — пользовательская функция C# (п.5.4 ТЗ)
- `FaultSummary` — строка под заголовком отчёта (отчёты 1 и 4): «Всего: N | Неисправно: N | X.X %»
- Кнопка «🖨 Печать» → `PrintDialog` → `PrintVisual` с масштабом под страницу
  Реализовано через `PrintRequested` (Action) из VM → `ReportsPage.PrintReport()` в code-behind
- `ChartBars` + `IsChartVisible` — горизонтальная диаграмма для отчёта 2

**Условное форматирование (`EquipmentPage.xaml`):**
- `DataGrid.RowStyle` с `DataTrigger`: Списано → красный фон, В ремонте → жёлтый

**Конвертеры:**
- `EmptyStringToVisibilityConverter` → ключ `StrToVis` (Visible когда строка не пустая)

**EquipmentDialog:**
- Автогенерация ИНВ-номера: `ИНВ-00001`, `ИНВ-00002`, ... (берёт max из БД + 1)
- `_saved` флаг + `OnWindowClosing` guard — спрашивает при закрытии без сохранения

**Убрано:**
- DEV-кнопка из `LoginWindow.xaml`
- `DevResetCommand` из `LoginViewModel`
- `DevResetPasswords()` из `AuthService`

**ToolTip** на кнопках: Добавить, Изменить, Удалить, Обновить, Печать, Экспорт

---

## Статус ТЗ — ВСЁ РЕАЛИЗОВАНО

| Пункт | Статус |
|---|---|
| 5.1 Стек (.NET8, WPF, MySQL, MVVM, BCrypt, ClosedXML) | ✅ |
| 5.2 Роли (admin/operator/observer) | ✅ |
| 5.3 Все 7 окон включая смену пароля | ✅ |
| 5.4 Все 5+ запросов включая перекрёстный + CalcFaultPercent | ✅ |
| 5.5 4 отчёта + диаграмма + условное форматирование | ✅ |
| 5.6 Журналирование всех событий + имя ПК | ✅ |
| 5.7 Бэкап, Excel, фото, валидация, ToolTip | ✅ |
| Пояснительная записка | ❌ не начата |

---

## Что передать следующей нейронке

```
Репозиторий: https://github.com/ggwpw/EquipmentDB
Токен: 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

git clone https://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@github.com/ggwpw/EquipmentDB.git

Прочитай первым: docs/obsidian/Meta/AI-Context.md

Стек: WPF / .NET 8 / MySQL 8 / EF Core (Pomelo) / MVVM / BCrypt / ClosedXML
Ключевые файлы:
  ViewModels/ReportsViewModel.cs   — отчёты, диаграмма, печать, CalcFaultPercent
  Views/Pages/ReportsPage.xaml     — страница отчётов с PrintArea
  Views/Pages/Pages.xaml.cs       — PrintReport() code-behind
  ViewModels/MainViewModel.cs      — навигация, ChangePasswordCommand
  Services/AuthService.cs          — Login, ChangePassword, WriteHistory
  Helpers/Helpers.cs               — PhoneMask, DataGridHelper, CurrentSession
  Converters/Converters.cs         — все конвертеры (PathToImage, StrToVis, etc.)
  Resources/Styles.xaml            — глобальные стили и конвертеры

Осталось только: пояснительная записка (ГОСТ 7.32-2017, 20+ стр.)
Текущая задача: [ВСТАВЬ ЗАДАЧУ]
```
