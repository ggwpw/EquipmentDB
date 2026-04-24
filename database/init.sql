CREATE DATABASE IF NOT EXISTS equipment_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE equipment_db;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE rooms (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    cabinet     VARCHAR(20)  NOT NULL UNIQUE,
    name        VARCHAR(100) NOT NULL,
    capacity    INT          NOT NULL DEFAULT 0,
    CONSTRAINT chk_capacity CHECK (capacity >= 0)
);

CREATE TABLE equipment_types (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    name        VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(255)
);

CREATE TABLE statuses (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    name        VARCHAR(50)  NOT NULL UNIQUE,
    color_hex   VARCHAR(7)   NOT NULL DEFAULT '#000000'
);

CREATE TABLE staff (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    full_name   VARCHAR(150) NOT NULL,
    position    VARCHAR(100),
    phone       VARCHAR(20),
    email       VARCHAR(100),
    room_id     INT,
    CONSTRAINT fk_staff_room FOREIGN KEY (room_id)
        REFERENCES rooms(id) ON DELETE SET NULL ON UPDATE CASCADE
);

CREATE TABLE users (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    login           VARCHAR(50)  NOT NULL UNIQUE,
    password_hash   VARCHAR(60)  NOT NULL,
    role            ENUM('admin','operator','observer') NOT NULL DEFAULT 'observer',
    staff_id        INT,
    is_active       TINYINT(1)   NOT NULL DEFAULT 1,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    failed_attempts TINYINT      NOT NULL DEFAULT 0,
    CONSTRAINT fk_users_staff FOREIGN KEY (staff_id)
        REFERENCES staff(id) ON DELETE SET NULL ON UPDATE CASCADE
);

CREATE TABLE equipment (
    id               INT AUTO_INCREMENT PRIMARY KEY,
    inventory_number VARCHAR(50)  NOT NULL UNIQUE,
    name             VARCHAR(150) NOT NULL,
    type_id          INT          NOT NULL,
    room_id          INT          NOT NULL,
    arrival_date     DATE         NOT NULL,
    status_id        INT          NOT NULL,
    specs            TEXT,
    photo_path       VARCHAR(500),
    CONSTRAINT fk_eq_type   FOREIGN KEY (type_id)   REFERENCES equipment_types(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_eq_room   FOREIGN KEY (room_id)   REFERENCES rooms(id)           ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_eq_status FOREIGN KEY (status_id) REFERENCES statuses(id)        ON DELETE RESTRICT ON UPDATE CASCADE
);

CREATE TABLE event_log (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    user_id     INT,
    event_type  VARCHAR(50)  NOT NULL,
    description VARCHAR(500) NOT NULL,
    table_name  VARCHAR(50),
    record_id   INT,
    event_time  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    machine_name VARCHAR(100),
    CONSTRAINT fk_log_user FOREIGN KEY (user_id)
        REFERENCES users(id) ON DELETE SET NULL ON UPDATE CASCADE
);

CREATE TABLE login_history (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    user_id     INT,
    login_time  DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    pc_name     VARCHAR(100),
    success     TINYINT(1)  NOT NULL DEFAULT 1,
    CONSTRAINT fk_lh_user FOREIGN KEY (user_id)
        REFERENCES users(id) ON DELETE SET NULL ON UPDATE CASCADE
);

CREATE INDEX idx_equipment_room       ON equipment(room_id);
CREATE INDEX idx_equipment_status     ON equipment(status_id);
CREATE INDEX idx_equipment_type       ON equipment(type_id);
CREATE INDEX idx_event_log_time       ON event_log(event_time);
CREATE INDEX idx_event_log_user       ON event_log(user_id);
CREATE INDEX idx_login_history_user   ON login_history(user_id);
CREATE INDEX idx_login_history_time   ON login_history(login_time);

SET FOREIGN_KEY_CHECKS = 1;

INSERT INTO rooms (cabinet, name, capacity) VALUES
    ('101', 'Компьютерный класс №1', 15),
    ('204', 'Компьютерный класс №2', 20),
    ('312', 'Лаборатория сетевых технологий', 12),
    ('115', 'Класс мультимедиа', 18);

INSERT INTO equipment_types (name, description) VALUES
    ('Системный блок',  'Настольный компьютер (системный блок)'),
    ('Монитор',         'Монитор для рабочей станции'),
    ('Принтер',         'Принтер лазерный или струйный'),
    ('Проектор',        'Мультимедийный проектор'),
    ('Маршрутизатор',   'Сетевое оборудование — маршрутизатор'),
    ('Коммутатор',      'Сетевое оборудование — коммутатор');

INSERT INTO statuses (name, color_hex) VALUES
    ('Исправно',   '#2E7D32'),
    ('В ремонте',  '#F57F17'),
    ('Списано',    '#C62828');

INSERT INTO staff (full_name, position, phone, email, room_id) VALUES
    ('Иванов Алексей Петрович',    'Системный администратор',  '+7-900-111-22-33', 'ivanov@school.ru',   1),
    ('Петрова Мария Сергеевна',    'Преподаватель информатики', '+7-900-444-55-66', 'petrova@school.ru',  2),
    ('Сидоров Дмитрий Олегович',   'Техник',                   '+7-900-777-88-99', 'sidorov@school.ru',  3),
    ('Козлова Наталья Викторовна', 'Преподаватель информатики', '+7-900-000-12-34', 'kozlova@school.ru',  4);

INSERT INTO users (login, password_hash, role, staff_id, is_active) VALUES
    ('admin',    '$2a$11$tvWqO5JSfYsfiDveXx3cPe9l0CCuC5iUDluLd5I5KwNJHzbFpZ3vW', 'admin',    1, 1),
    ('operator', '$2a$11$bsnsBo7BCyalgfVBc97OGeB6aTFBWNo5f1fFGMY.3CkyvqEQxPAzi', 'operator', 2, 1),
    ('observer', '$2a$11$aLc1ietXL0iW0SdLOhU/TeVcHCm9I4CIf68.X6.szifJVuv3z9Wgy', 'observer', 3, 1);

INSERT INTO equipment (inventory_number, name, type_id, room_id, arrival_date, status_id, specs) VALUES
    ('ИНВ-00001', 'ПК Rombica i5',          1, 1, '2021-09-01', 1, 'CPU: Intel Core i5-10400, RAM: 8 GB, SSD: 256 GB'),
    ('ИНВ-00002', 'ПК Rombica i5',          1, 1, '2021-09-01', 1, 'CPU: Intel Core i5-10400, RAM: 8 GB, SSD: 256 GB'),
    ('ИНВ-00003', 'ПК Rombica i5',          1, 1, '2021-09-01', 2, 'CPU: Intel Core i5-10400, RAM: 8 GB, SSD: 256 GB'),
    ('ИНВ-00004', 'Монитор Samsung 24"',    2, 1, '2021-09-01', 1, '24", 1920x1080, IPS, 60 Hz'),
    ('ИНВ-00005', 'Монитор Samsung 24"',    2, 1, '2021-09-01', 1, '24", 1920x1080, IPS, 60 Hz'),
    ('ИНВ-00006', 'ПК Lenovo ThinkCentre',  1, 2, '2022-01-15', 1, 'CPU: Intel Core i3-12100, RAM: 16 GB, SSD: 512 GB'),
    ('ИНВ-00007', 'ПК Lenovo ThinkCentre',  1, 2, '2022-01-15', 1, 'CPU: Intel Core i3-12100, RAM: 16 GB, SSD: 512 GB'),
    ('ИНВ-00008', 'ПК Lenovo ThinkCentre',  1, 2, '2022-01-15', 3, 'CPU: Intel Core i3-12100, RAM: 16 GB, SSD: 512 GB'),
    ('ИНВ-00009', 'Монитор LG 27"',         2, 2, '2022-01-15', 1, '27", 2560x1440, IPS, 75 Hz'),
    ('ИНВ-00010', 'Принтер HP LaserJet',    3, 2, '2020-06-10', 2, 'Лазерный, ч/б, A4, 28 стр/мин'),
    ('ИНВ-00011', 'Проектор Epson EB-X51',  4, 1, '2023-02-20', 1, '3LCD, 3800 лм, XGA 1024x768'),
    ('ИНВ-00012', 'Проектор Epson EB-X51',  4, 2, '2023-02-20', 1, '3LCD, 3800 лм, XGA 1024x768'),
    ('ИНВ-00013', 'Маршрутизатор MikroTik', 5, 3, '2022-09-01', 1, 'RB960PGS, 5x Gigabit, SFP'),
    ('ИНВ-00014', 'Коммутатор D-Link 24p',  6, 3, '2022-09-01', 1, '24x Fast Ethernet, 2x Gigabit uplink'),
    ('ИНВ-00015', 'ПК Aquarius Pro',         1, 4, '2023-08-28', 1, 'CPU: AMD Ryzen 5 5600G, RAM: 16 GB, SSD: 512 GB'),
    ('ИНВ-00016', 'Монитор BenQ 24"',       2, 4, '2023-08-28', 1, '24", 1920x1080, VA, 75 Hz'),
    ('ИНВ-00017', 'Принтер Canon i-SENSYS', 3, 4, '2021-03-05', 3, 'Лазерный МФУ, цветной, A4');
