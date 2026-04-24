using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace EquipmentDB.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
public class HexToBrushConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
    {
        if (v is string hex) try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); } catch { }
        return Brushes.Gray;
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
public class RoleConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v?.ToString() switch { "admin" => "Администратор", "operator" => "Оператор", "observer" => "Наблюдатель", _ => v?.ToString() ?? "" };
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
public class ActiveConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is true ? "Активен" : "Заблокирован";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
public class ActiveBrushConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is true ? new SolidColorBrush(Color.FromRgb(39,174,96)) : new SolidColorBrush(Color.FromRgb(231,76,60));
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v != null ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
public class DateOnlyConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is DateOnly d ? d.ToDateTime(TimeOnly.MinValue) : DateTime.Today;
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => v is DateTime dt ? DateOnly.FromDateTime(dt) : DateOnly.FromDateTime(DateTime.Today);
}
public class SuccessConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is true ? "Успешно" : "Неудача";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
