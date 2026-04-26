using System.Windows.Controls;
using System.Printing;
using EquipmentDB.ViewModels;

namespace EquipmentDB.Views.Pages;

public partial class EquipmentPage : UserControl { public EquipmentPage() => InitializeComponent(); }
public partial class RoomsPage    : UserControl { public RoomsPage()    => InitializeComponent(); }
public partial class UsersPage    : UserControl { public UsersPage()    => InitializeComponent(); }
public partial class EventLogPage : UserControl { public EventLogPage() => InitializeComponent(); }
public partial class ReportsPage  : UserControl
{
    public ReportsPage()
    {
        InitializeComponent();
        // Подключаем делегат печати после инициализации DataContext
        Loaded += (_, _) =>
        {
            if (DataContext is ReportsViewModel vm)
                vm.PrintRequested = PrintReport;
        };
    }

    // Вызывается из ReportsViewModel через команду
    internal void PrintReport()
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;

        // Масштабируем PrintArea под страницу принтера
        var area = PrintArea;
        var capabilities = dlg.PrintQueue.GetPrintCapabilities(dlg.PrintTicket);
        double printW = capabilities.PageImageableArea?.ExtentWidth  ?? dlg.PrintableAreaWidth;
        double printH = capabilities.PageImageableArea?.ExtentHeight ?? dlg.PrintableAreaHeight;

        double scale = Math.Min(printW / area.ActualWidth, printH / area.ActualHeight);
        area.RenderTransform = new System.Windows.Media.ScaleTransform(scale, scale);

        dlg.PrintVisual(area, ((ReportsViewModel)DataContext).ReportTitle);

        area.RenderTransform = System.Windows.Media.Transform.Identity;
    }
}
