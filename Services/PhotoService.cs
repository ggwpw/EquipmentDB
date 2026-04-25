using System.IO;

namespace EquipmentDB.Services;

public static class PhotoService
{
    // Папка рядом с exe: Photos/
    public static string PhotosDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos");

    /// <summary>
    /// Копирует файл в папку Photos, возвращает относительный путь вида "Photos\ИНВ-00001.jpg"
    /// </summary>
    public static string CopyPhoto(string sourcePath, string inventoryNumber)
    {
        Directory.CreateDirectory(PhotosDir);

        var ext  = Path.GetExtension(sourcePath).ToLowerInvariant();
        // Имя файла = инв.номер, спецсимволы заменяем на подчёркивание
        var safe = string.Join("_", inventoryNumber.Split(Path.GetInvalidFileNameChars()));
        var dest = Path.Combine(PhotosDir, safe + ext);

        File.Copy(sourcePath, dest, overwrite: true);
        // Сохраняем относительный путь — работает на любом ПК в сети
        return Path.Combine("Photos", safe + ext);
    }

    /// <summary>
    /// Возвращает абсолютный путь для отображения из относительного, хранящегося в БД
    /// </summary>
    public static string? ResolveAbsolutePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        // Если уже абсолютный (старые записи) — вернуть как есть
        if (Path.IsPathRooted(relativePath)) return relativePath;
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
    }

    public static void DeletePhoto(string? relativePath)
    {
        var full = ResolveAbsolutePath(relativePath);
        if (full != null && File.Exists(full))
            try { File.Delete(full); } catch { }
    }
}
