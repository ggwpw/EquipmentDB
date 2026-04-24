# 📊 Отчёты

**Расположение:** `ViewModels/ReportsViewModel.cs` → метод `LoadReportAsync()`  
**Рендер:** `Views/Pages/ReportsPage.xaml` с `DataGrid AutoGenerateColumns="True"`

---

## Отчёт 0 — Инвентарная ведомость по классам

**Название в UI:** "Инвентарная ведомость по классам"  
**Группировка:** по кабинету, затем по инвентарному номеру  
**Колонки:** Класс, Инв.номер, Название, Тип, Состояние, Дата  
**Источник:** `equipment` JOIN `rooms` + `equipment_types` + `statuses`

```csharp
ctx.Equipment.Include(e => e.Room).Include(e => e.Type).Include(e => e.Status)
    .OrderBy(e => e.Room.Cabinet).ThenBy(e => e.InventoryNumber)
    .Select(e => new { Класс = ..., Инв_номер = ..., ... })
```

---

## Отчёт 1 — Сводка по состоянию оборудования

**Название в UI:** "Сводка по состоянию оборудования"  
**Группировка:** по статусу  
**Колонки:** Состояние, Количество  
**Запрос:** GROUP BY status_id

```csharp
ctx.Equipment.GroupBy(e => e.Status.Name)
    .Select(g => new { Состояние = g.Key, Количество = g.Count() })
```

---

## Отчёт 2 — Аналитика по типам оборудования

**Название в UI:** "Аналитика по типам оборудования"  
**Сортировка:** по убыванию общего количества  
**Колонки:** Тип, Всего, Исправно, В_ремонте, Списано  
**Запрос:** GROUP BY type_id с подсчётами по статусам

```csharp
ctx.Equipment.GroupBy(e => e.Type.Name)
    .Select(g => new { Тип = g.Key, Всего = g.Count(),
        Исправно   = g.Count(e => e.Status.Name == "Исправно"),
        В_ремонте  = g.Count(e => e.Status.Name == "В ремонте"),
        Списано    = g.Count(e => e.Status.Name == "Списано") })
    .OrderByDescending(g => g.Всего)
```

---

## Отчёт 3 — Журнал событий за 7 дней

**Название в UI:** "Журнал событий за последние 7 дней"  
**Фильтр:** `event_time >= DateTime.Today.AddDays(-7)`  
**Колонки:** Время, Пользователь, Событие, Описание  
**Сортировка:** по убыванию времени

---

## Экспорт в Excel

**Сервис:** `Services/ExcelService.cs` → `ExportEquipment(List<Equipment> items, string path)`  
**Библиотека:** ClosedXML  
**Вызов:** кнопка «📥 Excel» на странице Оборудования  
**Диалог:** `SaveFileDialog` с фильтром `*.xlsx`  
**Содержимое:** текущий список (с учётом фильтров) → 8 колонок  
**После экспорта:** запись в `event_log` через `LogService.Log()`
