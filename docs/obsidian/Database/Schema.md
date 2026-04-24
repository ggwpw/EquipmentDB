# 🗄 Схема базы данных

**СУБД:** MySQL 8  
**База:** `equipment_db`  
**Кодировка:** `utf8mb4_unicode_ci`  
**Скрипт создания:** `init.sql`

---

## Таблицы и связи

```
rooms ──────────────── staff ──── users
  │                      │
  └── equipment ─── equipment_types
          │
          └── statuses

users ─── event_log
users ─── login_history
```

---

## rooms

| Поле | Тип | Описание |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| cabinet | VARCHAR(20) UNIQUE NOT NULL | Номер кабинета: "101" |
| name | VARCHAR(100) NOT NULL | Название: "Компьютерный класс №1" |
| capacity | INT DEFAULT 0 | Количество мест |

**Тестовые данные:** 4 записи (101, 204, 312, 115)

---

## equipment_types

| Поле | Тип | Описание |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| name | VARCHAR(100) UNIQUE NOT NULL | "Системный блок", "Монитор" и т.д. |
| description | VARCHAR(255) | Опциональное описание |

**Тестовые данные:** 6 типов (Системный блок, Монитор, Принтер, Проектор, Маршрутизатор, Коммутатор)

---

## statuses

| Поле | Тип | Описание |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| name | VARCHAR(50) UNIQUE NOT NULL | |
| color_hex | VARCHAR(7) DEFAULT '#000000' | Цвет точки в UI |

**Тестовые данные:**

| id | name | color_hex |
|---|---|---|
| 1 | Исправно | #2E7D32 (зелёный) |
| 2 | В ремонте | #F57F17 (жёлтый) |
| 3 | Списано | #C62828 (красный) |

---

## staff

| Поле | Тип | Описание |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| full_name | VARCHAR(150) NOT NULL | ФИО |
| position | VARCHAR(100) | Должность |
| phone | VARCHAR(20) | |
| email | VARCHAR(100) | |
| room_id | INT FK → rooms(id) | ON DELETE SET NULL |

---

## users

| Поле | Тип | Описание |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| login | VARCHAR(50) UNIQUE NOT NULL | |
| password_hash | VARCHAR(60) NOT NULL | BCrypt $2b$11$... |
| role | ENUM('admin','operator','observer') | |
| staff_id | INT FK → staff(id) | ON DELETE SET NULL |
| is_active | TINYINT(1) DEFAULT 1 | 0 = заблокирован |
| created_at | DATETIME DEFAULT NOW() | |
| failed_attempts | TINYINT DEFAULT 0 | Блок при ≥ 3 |

**Тестовые пользователи:**

| login | password | role |
|---|---|---|
| admin | admin123 | admin |
| operator | oper123 | operator |
| observer | obs123 | observer |

---

## equipment

| Поле | Тип | Описание |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| inventory_number | VARCHAR(50) UNIQUE NOT NULL | "ИНВ-00001" |
| name | VARCHAR(150) NOT NULL | "ПК Rombica i5" |
| type_id | INT FK → equipment_types(id) | ON DELETE RESTRICT |
| room_id | INT FK → rooms(id) | ON DELETE RESTRICT |
| arrival_date | DATE NOT NULL | |
| status_id | INT FK → statuses(id) | ON DELETE RESTRICT |
| specs | TEXT | Характеристики (свободный текст) |
| photo_path | VARCHAR(500) | Путь к файлу на диске |

**Тестовые данные:** 17 записей с разными статусами

---

## event_log

| Поле | Тип | Описание |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| user_id | INT FK → users(id) | ON DELETE SET NULL, может быть NULL |
| event_type | VARCHAR(50) NOT NULL | "Добавление", "Удаление" и т.д. |
| description | VARCHAR(500) NOT NULL | Детали события |
| table_name | VARCHAR(50) | Затронутая таблица |
| record_id | INT | ID затронутой записи |
| event_time | DATETIME DEFAULT NOW() | |

**Стандартные типы событий:** см. [[../Meta/Conventions#типы-событий-журнала]]

---

## login_history

| Поле | Тип | Описание |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| user_id | INT FK → users(id) | ON DELETE SET NULL |
| login_time | DATETIME DEFAULT NOW() | |
| pc_name | VARCHAR(100) | `Environment.MachineName` |
| success | TINYINT(1) NOT NULL | 1 = успех, 0 = неудача |

---

## Индексы

```sql
idx_equipment_room     ON equipment(room_id)
idx_equipment_status   ON equipment(status_id)
idx_equipment_type     ON equipment(type_id)
idx_event_log_time     ON event_log(event_time)
idx_event_log_user     ON event_log(user_id)
idx_login_history_user ON login_history(user_id)
idx_login_history_time ON login_history(login_time)
```

---

## Маппинг EF Core → MySQL

`UseSnakeCaseNamingConvention()` — все поля автоматически в snake_case.  
Таблицы прописаны явно в `OnModelCreating()`:

```csharp
m.Entity<Room>().ToTable("rooms");
m.Entity<EquipmentType>().ToTable("equipment_types");
// и т.д.
```
