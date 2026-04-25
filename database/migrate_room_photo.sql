-- Миграция: добавить photo_path в таблицу rooms
ALTER TABLE rooms
    ADD COLUMN IF NOT EXISTS photo_path VARCHAR(500) AFTER capacity;
