# 🔐 Роли и права доступа

---

## Матрица прав

| Функция | admin | operator | observer |
|---|:---:|:---:|:---:|
| Просмотр оборудования | ✅ | ✅ | ✅ |
| Добавление оборудования | ✅ | ✅ | ❌ |
| Редактирование оборудования | ✅ | ✅ | ❌ |
| Удаление оборудования | ✅ | ✅ | ❌ |
| Просмотр справочников (классы, сотрудники) | ✅ | ✅ | ✅ |
| Редактирование справочников | ✅ | ✅ | ❌ |
| Управление пользователями | ✅ | ❌ | ❌ |
| Журнал событий | ✅ | ❌ | ❌ |
| Отчёты | ✅ | ✅ | ✅ |
| Экспорт Excel | ✅ | ✅ | ✅ |
| Резервное копирование | ✅ | ❌ | ❌ |
| Смена своего пароля | ✅ | ✅ | ✅ |
| Сброс чужого пароля | ✅ | ❌ | ❌ |

---

## Реализация в коде

### Проверка роли

```csharp
// Helpers/Helpers.cs — CurrentSession
CurrentSession.IsAdmin    // role == "admin"
CurrentSession.IsOperator // role == "operator"
CurrentSession.CanEdit    // IsAdmin || IsOperator
```

### Скрытие в UI (XAML)

```xml
<!-- Только для редакторов (admin + operator) -->
<Button Visibility="{Binding CanEdit, Converter={StaticResource BoolToVis}}"/>

<!-- Только для администратора -->
<Button Visibility="{Binding IsAdmin, Converter={StaticResource BoolToVis}}"/>

<!-- Пункты навигации Пользователи и Журнал — MainWindow.xaml -->
<Button Command="{Binding NavUsersCommand}"
        Visibility="{Binding IsAdmin, Converter={StaticResource BoolToVis}}"/>
```

### CanExecute в командах

```csharp
// EquipmentViewModel
AddCommand    = new RelayCommand(_ => OpenDialog(null),     _ => CanEdit);
EditCommand   = new RelayCommand(_ => OpenDialog(Selected), _ => Selected != null && CanEdit);
DeleteCommand = new RelayCommand(_ => DeleteSelected(),     _ => Selected != null && CanEdit);
```

---

## Блокировка учётной записи

- Поле `failed_attempts` в таблице `users`
- При каждой неудачной попытке: `user.FailedAttempts++`
- При `FailedAttempts >= 3`: `user.IsActive = false`
- Разблокировка: только `admin` через кнопку «Блок/Разблок» → сбрасывает `failed_attempts = 0`, `is_active = 1`
- Попытка войти с заблокированным аккаунтом → `LoginResult.AccountLocked` → сообщение "Учётная запись заблокирована."
