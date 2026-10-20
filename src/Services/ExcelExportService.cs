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
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    private static void WriteTxSheet(XLWorkbook workbook, ObservableCollection<TxTestResult> results)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    private static void WriteHarmonicSheet(XLWorkbook workbook, ObservableCollection<HarmonicTestResult> results)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }
}
