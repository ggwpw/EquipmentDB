using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EquipmentDB.Models;

namespace EquipmentDB.Helpers;

/// <summary>
/// Attached behavior: форматирует ввод в маску +7 (XXX) XXX-XX-XX
/// Использование: helpers:PhoneMask.IsEnabled="True" на TextBox
/// </summary>
public static class PhoneMask
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(PhoneMask),
            new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetIsEnabled(TextBox tb) => (bool)tb.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(TextBox tb, bool v) => tb.SetValue(IsEnabledProperty, v);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;
        if ((bool)e.NewValue)
        {
            tb.PreviewTextInput += OnPreviewTextInput;
            tb.PreviewKeyDown   += OnPreviewKeyDown;
            tb.GotFocus         += OnGotFocus;
            DataObject.AddPastingHandler(tb, OnPaste);
        }
        else
        {
            tb.PreviewTextInput -= OnPreviewTextInput;
            tb.PreviewKeyDown   -= OnPreviewKeyDown;
            tb.GotFocus         -= OnGotFocus;
            DataObject.RemovePastingHandler(tb, OnPaste);
        }
    }

    // +7 (___) ___-__-__  — позиции цифр: 4,5,6, 9,10,11, 13,14, 16,17
    private static readonly int[] DigitPositions = { 4, 5, 6, 9, 10, 11, 13, 14, 16, 17 };

    private static string FormatPhone(string digits)
    {
        // digits — только цифры, максимум 10 знаков (без кода страны)
        if (digits.Length > 0 && digits[0] == '7') digits = digits[1..]; // убрать 7 если вставили
        if (digits.Length > 10) digits = digits[..10];

        char[] mask = "+7 (___) ___-__-__".ToCharArray();
        int di = 0;
        for (int i = 0; i < mask.Length && di < digits.Length; i++)
            if (mask[i] == '_') mask[i] = digits[di++];
        return new string(mask);
    }

    private static string ExtractDigits(string text)
    {
        var sb = new StringBuilder();
        foreach (char c in text) if (char.IsDigit(c)) sb.Append(c);
        var s = sb.ToString();
        if (s.Length > 0 && s[0] == '7') s = s[1..]; // strip leading 7
        return s.Length > 10 ? s[..10] : s;
    }

    private static void Apply(TextBox tb, string rawDigits)
    {
        int caretOffset = rawDigits.Length; // цифра которую добавляем
        tb.Text = FormatPhone(rawDigits);
        // ставим курсор после последней введённой цифры
        if (caretOffset < DigitPositions.Length)
            tb.CaretIndex = DigitPositions[caretOffset];
        else
            tb.CaretIndex = tb.Text.Length;
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox tb) return;
        e.Handled = true;
        if (!char.IsDigit(e.Text[0])) return;

        var digits = ExtractDigits(tb.Text) + e.Text;
        if (digits.Length > 10) return;
        Apply(tb, digits);
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb) return;
        if (e.Key == Key.Back)
        {
            e.Handled = true;
            var digits = ExtractDigits(tb.Text);
            if (digits.Length == 0) return;
            Apply(tb, digits[..^1]);
        }
        else if (e.Key == Key.Delete)
        {
            e.Handled = true;
        }
    }

    private static void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && string.IsNullOrWhiteSpace(ExtractDigits(tb.Text)))
            tb.Text = FormatPhone(""); // показываем пустую маску
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox tb) return;
        e.CancelCommand();
        if (e.DataObject.GetData(typeof(string)) is string pasted)
        {
            var digits = ExtractDigits(pasted);
            if (digits.Length > 0) Apply(tb, digits);
        }
    }
}

/// <summary>
/// Attached behavior: добавляет контекстное меню с копированием строки для DataGrid
/// Использование: helpers:DataGridHelper.EnableCopyRow="True"
/// </summary>
public static class DataGridHelper
{
    public static readonly DependencyProperty EnableCopyRowProperty =
        DependencyProperty.RegisterAttached("EnableCopyRow", typeof(bool), typeof(DataGridHelper),
            new PropertyMetadata(false, OnEnableCopyRowChanged));

    public static bool GetEnableCopyRow(DataGrid dg) => (bool)dg.GetValue(EnableCopyRowProperty);
    public static void SetEnableCopyRow(DataGrid dg, bool v) => dg.SetValue(EnableCopyRowProperty, v);

    private static void OnEnableCopyRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dg || !(bool)e.NewValue) return;

        var copyItem = new MenuItem { Header = "📋  Копировать строку" };
        copyItem.Click += (_, _) => CopySelectedRow(dg);

        var menu = new ContextMenu();
        menu.Items.Add(copyItem);
        dg.ContextMenu = menu;

        // обновить IsEnabled при смене выбранного элемента
        dg.SelectionChanged += (_, _) =>
            copyItem.IsEnabled = dg.SelectedItem != null;
        copyItem.IsEnabled = false;
    }

    private static void CopySelectedRow(DataGrid dg)
    {
        if (dg.SelectedItem == null) return;
        var parts = new List<string>();
        foreach (var col in dg.Columns)
        {
            if (col is not DataGridTextColumn tc) continue;
            string header = tc.Header?.ToString() ?? "";
            string val    = GetValueViaBindingPath(dg.SelectedItem, tc) ?? "";
            parts.Add($"{header}: {val}");
        }
        if (parts.Count > 0)
            Clipboard.SetText(string.Join("\t", parts));
    }

    private static string? GetValueViaBindingPath(object item, DataGridTextColumn col)
    {
        try
        {
            var binding = col.Binding as System.Windows.Data.Binding;
            if (binding?.Path?.Path == null) return null;
            var parts = binding.Path.Path.Split('.');
            object? current = item;
            foreach (var part in parts)
            {
                if (current == null) return null;
                var prop = current.GetType().GetProperty(part);
                current = prop?.GetValue(current);
            }
            // Apply StringFormat if present
            if (binding.StringFormat != null && current != null)
                return string.Format(binding.StringFormat, current);
            return current?.ToString();
        }
        catch { return null; }
    }
}


public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    { _execute = execute; _canExecute = canExecute; }
    public bool CanExecute(object? p) => _canExecute?.Invoke(p) ?? true;
    public void Execute(object? p) => _execute(p);
}

public static class CurrentSession
{
    public static User? User { get; set; }
    public static bool IsAdmin => User?.Role == "admin";
    public static bool IsOperator => User?.Role == "operator";
    public static bool CanEdit => IsAdmin || IsOperator;
    public static void Clear() => User = null;
}
