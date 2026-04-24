# 📐 Соглашения проекта

> Обязательно к прочтению перед внесением изменений.  
> Нарушение соглашений = конфликты при слиянии и путаница у AI.

---

## Соглашения по коду C#

### Именование
```csharp
// Приватные поля — camelCase с подчёркиванием
private string _login = string.Empty;
private bool _isLoading;

// Публичные свойства — PascalCase
public string Login { get => _login; set => Set(ref _login, value); }

// Команды — PascalCase + суффикс Command
public ICommand SaveCommand { get; }
public ICommand DeleteCommand { get; }

// Async методы — суффикс Async
public async Task LoadAsync() { ... }
```

### ViewModel — обязательный шаблон
```csharp
public class MyViewModel : BaseViewModel
{
    private string _field = string.Empty;
    
    // 1. Свойства с Set()
    public string Field { get => _field; set => Set(ref _field, value); }
    
    // 2. Команды в конструкторе
    public ICommand MyCommand { get; }
    
    public MyViewModel()
    {
        MyCommand = new RelayCommand(_ => DoSomething(), _ => CanDoSomething());
        _ = LoadAsync(); // если нужна загрузка данных
    }
    
    // 3. Async загрузка отдельным методом
    public async Task LoadAsync() { ... }
}
```

### Работа с БД
```csharp
// ВСЕГДА using — контекст не хранить в полях ViewModel
using var ctx = new AppDbContext();

// Async для загрузки списков
await using var ctx = new AppDbContext();
var items = await ctx.Equipment.Include(e => e.Type).ToListAsync();

// Sync для простых операций (CRUD в диалогах)
using var ctx = new AppDbContext();
ctx.Equipment.Add(eq);
ctx.SaveChanges();
```

### Журналирование — ОБЯЗАТЕЛЬНО после каждого изменения БД
```csharp
// Формат: Тип события, описание с деталями, таблица, id записи
LogService.Log("Добавление", $"Добавлено оборудование: {eq.InventoryNumber} — {eq.Name}", "equipment", eq.Id);
LogService.Log("Изменение",  $"Изменено: {old} → {eq.Name}", "equipment", eq.Id);
LogService.Log("Удаление",   $"Удалено оборудование: {eq.InventoryNumber}", "equipment", eq.Id);
```

### Типы событий журнала (стандартные строки)
```
"Добавление"
"Изменение"  
"Удаление"
"Вход"
"Выход"
"Блокировка"
"Разблокировка"
"Смена пароля"
"Сброс пароля"
"Резервное копирование"
"Экспорт Excel"
```

---

## Соглашения по XAML

### Кнопки — только через стили
```xml
<!-- Основное действие -->
<Button Content="Сохранить" Style="{StaticResource BtnPrimary}"/>

<!-- Деструктивное действие -->
<Button Content="Удалить" Style="{StaticResource BtnDanger}"/>

<!-- Вторичное / нейтральное -->
<Button Content="Отмена" Style="{StaticResource BtnSecondary}"/>

<!-- Навигация в сайдбаре -->
<Button Content="Оборудование" Style="{StaticResource BtnNav}"/>
```

### Видимость по роли
```xml
<!-- Скрывать кнопки редактирования от наблюдателей -->
<Button Visibility="{Binding CanEdit, Converter={StaticResource BoolToVis}}"/>

<!-- Скрывать от не-администраторов -->
<Button Visibility="{Binding IsAdmin, Converter={StaticResource BoolToVis}}"/>
```

### Карточки (белые блоки с тенью)
```xml
<Border Style="{StaticResource Card}">
    <!-- контент -->
</Border>

<!-- Карточка без внутренних отступов (для DataGrid) -->
<Border Style="{StaticResource Card}" Padding="0">
    <DataGrid .../>
</Border>
```

### Метки полей форм
```xml
<TextBlock Text="Название поля *" Style="{StaticResource FormLabel}"/>
<TextBox Text="{Binding Property, UpdateSourceTrigger=PropertyChanged}"/>
```

### Цветовая схема (только эти ключи)
```xml
{StaticResource PrimaryBrush}       <!-- #1E3A5F тёмно-синий -->
{StaticResource PrimaryLightBrush}  <!-- #2D5FA6 средне-синий -->
{StaticResource AccentBrush}        <!-- #3A7BD5 яркий синий -->
{StaticResource SurfaceBrush}       <!-- #F5F7FA светло-серый фон -->
{StaticResource BorderBrush2}       <!-- #DDE3ED граница -->
{StaticResource TextPrimaryBrush}   <!-- #1A1A2E основной текст -->
{StaticResource TextMutedBrush}     <!-- #6B7A99 второстепенный текст -->
{StaticResource SuccessBrush}       <!-- #27AE60 зелёный -->
{StaticResource DangerBrush}        <!-- #E74C3C красный -->
```

---

## Соглашения по документации (этот vault)

### Структура файла
```markdown
# Заголовок

> Краткое описание одной строкой

---

## Раздел

Содержимое.
```

### Статусы задач
```markdown
- [ ] Не начато
- [~] В процессе  
- [x] Готово
- [!] Проблема / заблокировано
```

### Ошибки в Error-Log
```markdown
## ERR-XXX: Краткое название

**Статус:** 🔴 Активна / 🟡 В работе / 🟢 Решена  
**Файл:** `путь/к/файлу.cs` строка N  
**Описание:** что происходит  
**Воспроизведение:** как воспроизвести  
**Причина:** почему происходит (если известно)  
**Решение:** что сделали  
**Дата:** YYYY-MM-DD
```

### Обновление статуса проекта
После каждой рабочей сессии обновлять чеклист в [[../README]].

---

## Запрещено

- ❌ Хранить строку подключения в коде — только в `config.json`
- ❌ Хранить пароли в открытом виде — только BCrypt hash
- ❌ Создавать новые стили в XAML минуя `Styles.xaml`
- ❌ Использовать `MessageBox` без обработки ответа (для деструктивных операций)
- ❌ Делать CRUD без последующего вызова `LogService.Log()`
- ❌ Держать `AppDbContext` как поле класса — только `using var ctx = new AppDbContext()`
