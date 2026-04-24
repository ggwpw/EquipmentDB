using ClosedXML.Excel;
using EquipmentDB.Models;

namespace EquipmentDB.Services;

public static class ExcelService
{
    public static void ExportEquipment(List<Equipment> items, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Оборудование");

        string[] headers = ["№", "Инв. номер", "Название", "Тип", "Класс", "Дата поступления", "Состояние", "Характеристики"];
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        for (int r = 0; r < items.Count; r++)
        {
            var eq = items[r];
            ws.Cell(r+2,1).Value = r+1;
            ws.Cell(r+2,2).Value = eq.InventoryNumber;
            ws.Cell(r+2,3).Value = eq.Name;
            ws.Cell(r+2,4).Value = eq.Type?.Name ?? "";
            ws.Cell(r+2,5).Value = eq.Room?.Name ?? "";
            ws.Cell(r+2,6).Value = eq.ArrivalDate.ToString("dd.MM.yyyy");
            ws.Cell(r+2,7).Value = eq.Status?.Name ?? "";
            ws.Cell(r+2,8).Value = eq.Specs ?? "";
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
        LogService.Log("Экспорт Excel", $"Экспортировано {items.Count} записей", "equipment");
    }
}
