-- Миграция: добавить machine_name в event_log (для существующих БД)
-- Запускать только если база уже создана через init.sql
ALTER TABLE event_log
    ADD COLUMN IF NOT EXISTS machine_name VARCHAR(100) AFTER event_time;
