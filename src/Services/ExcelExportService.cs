using System.Collections.ObjectModel;
using ClosedXML.Excel;
using WIFInonSignalingTool.Models;

namespace WIFInonSignalingTool.Services;

public static class ExcelExportService
{
    public static void ExportReport(
        string filePath,
        ObservableCollection<TxTestResult> txResults,
        ObservableCollection<HarmonicTestResult> harmonicResults,
        string cmwIdn,
        string n9020aIdn,
        double cableLoss,
        double harmonicLossFund,
        double harmonicLossH2)
    {
        using var workbook = new XLWorkbook();

        var info = workbook.Worksheets.Add("Info");
        info.Cell(1, 1).Value = "WiFi Non-Signaling Test Report";
        info.Cell(1, 1).Style.Font.Bold = true;
        info.Cell(1, 1).Style.Font.FontSize = 14;
        info.Range(1, 1, 1, 2).Merge();

        info.Cell(3, 1).Value = "Generated";
        info.Cell(3, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        info.Cell(4, 1).Value = "CMW500 IDN";
        info.Cell(4, 2).Value = cmwIdn;
        info.Cell(5, 1).Value = "Cable Loss (dB)";
        info.Cell(5, 2).Value = cableLoss;

        int infoRow = 6;
        if (!string.IsNullOrEmpty(n9020aIdn))
        {
            info.Cell(infoRow, 1).Value = "N9020A IDN";
            info.Cell(infoRow, 2).Value = n9020aIdn;
            infoRow++;
            info.Cell(infoRow, 1).Value = "Harmonic Loss Fund (dB)";
            info.Cell(infoRow, 2).Value = harmonicLossFund;
            infoRow++;
            info.Cell(infoRow, 1).Value = "Harmonic Loss H2 (dB)";
            info.Cell(infoRow, 2).Value = harmonicLossH2;
            infoRow++;
        }

        info.Columns().AdjustToContents();
        info.Range(3, 1, infoRow - 1, 1).Style.Font.Bold = true;

        if (txResults.Count > 0)
            WriteTxSheet(workbook, txResults);

        if (harmonicResults.Count > 0)
            WriteHarmonicSheet(workbook, harmonicResults);

        workbook.SaveAs(filePath);
    }

    private static void WriteTxSheet(XLWorkbook workbook, ObservableCollection<TxTestResult> results)
    {
        var ws = workbook.Worksheets.Add("TX");
        string[] headers = {
            "Mode", "BW", "Rate", "Channel", "Freq (MHz)", "Gain",
            "Burst Power (dBm)", "EVM RMS", "Freq Error (Hz)",
            "Status"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1976D2");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int row = 2;
        foreach (var r in results)
        {
            ws.Cell(row, 1).Value = r.Mode;
            ws.Cell(row, 2).Value = r.Bandwidth;
            ws.Cell(row, 3).Value = r.Rate;
            ws.Cell(row, 4).Value = r.Channel;
            ws.Cell(row, 5).Value = r.Frequency;
            ws.Cell(row, 6).Value = r.Gain;
            ws.Cell(row, 7).Value = r.BurstPower;
            ws.Cell(row, 8).Value = r.EvmRms;
            ws.Cell(row, 9).Value = r.FreqError;
            ws.Cell(row, 10).Value = r.Status;

            var statusCell = ws.Cell(row, 10);
            statusCell.Style.Font.Bold = true;
            statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            statusCell.Style.Font.FontColor = r.Status switch
            {
                "Done" => XLColor.FromHtml("#388E3C"),
                "Error" => XLColor.FromHtml("#D32F2F"),
                "No Signal" => XLColor.FromHtml("#F57C00"),
                _ => XLColor.Gray
            };
            row++;
        }

        var dataRange = ws.Range(1, 1, row - 1, headers.Length);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Range(2, 1, row - 1, headers.Length).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Columns().AdjustToContents();
    }

    private static void WriteHarmonicSheet(XLWorkbook workbook, ObservableCollection<HarmonicTestResult> results)
    {
        var ws = workbook.Worksheets.Add("Harmonic");
        string[] headers = {
            "Mode", "BW", "Channel", "Freq (MHz)", "Gain",
            "Fund Raw (dBm)", "Fund DUT (dBm)",
            "H2 Freq (MHz)", "H2 (dBc)", "H2 (dBm)",
            "THD (%)", "Status"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#7B1FA2");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int row = 2;
        foreach (var r in results)
        {
            ws.Cell(row, 1).Value = r.Mode;
            ws.Cell(row, 2).Value = r.Bandwidth;
            ws.Cell(row, 3).Value = r.Channel;
            ws.Cell(row, 4).Value = r.Frequency;
            ws.Cell(row, 5).Value = r.Gain;
            ws.Cell(row, 6).Value = r.FundPowerRaw;
            ws.Cell(row, 7).Value = r.FundPowerDut;
            ws.Cell(row, 8).Value = r.H2FreqMHz;
            ws.Cell(row, 9).Value = r.H2PowerDbc;
            ws.Cell(row, 10).Value = r.H2PowerDbm;
            ws.Cell(row, 11).Value = r.ThdPercent;
            ws.Cell(row, 12).Value = r.Status;

            var statusCell = ws.Cell(row, 12);
            statusCell.Style.Font.Bold = true;
            statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            statusCell.Style.Font.FontColor = r.Status switch
            {
                "Done" => XLColor.FromHtml("#388E3C"),
                "Error" => XLColor.FromHtml("#D32F2F"),
                _ => XLColor.Gray
            };
            row++;
        }

        var dataRange = ws.Range(1, 1, row - 1, headers.Length);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Range(2, 1, row - 1, headers.Length).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Columns().AdjustToContents();
    }
}
