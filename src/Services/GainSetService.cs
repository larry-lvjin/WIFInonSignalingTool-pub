using System.IO;
using ClosedXML.Excel;
using WIFInonSignalingTool.Models;

namespace WIFInonSignalingTool.Services;

/// <summary>
/// 增益设定文件（gainset.xlsx）的导入/导出/持久化。
/// 文件格式：3 列 —— 速率 / 信道号 / gain，行顺序必须与 WifiTestConfigs 默认配置一致（21 行）。
/// </summary>
public static class GainSetService
{
    private const string HeaderRate = "速率";
    private const string HeaderChannel = "信道号";
    private const string HeaderGain = "gain";

    public static string PersistedFilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WIFInonSignalingTool");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "gainset.xlsx");
        }
    }

    /// <summary>随程序发行的默认 gainset.xlsx（位于 exe 同目录），用户未导入自定义文件时使用。</summary>
    public static string BundledDefaultFilePath =>
        Path.Combine(AppContext.BaseDirectory, "gainset.xlsx");

    /// <summary>导出模板：把 configs 中的 Rate/Channel/Gain 按行序写出。</summary>
    public static void ExportTemplate(string filePath, List<WifiTestItem> configs)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    /// <summary>读取 xlsx 中的 gain 列（按行顺序），返回 null 表示文件缺失或格式非法。</summary>
    public static List<int>? ReadGainsInOrder(string filePath)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    /// <summary>把用户选择的文件复制到持久化位置，返回该位置读取到的 gain 列表；失败返回 null。</summary>
    public static List<int>? ImportAndPersist(string sourceFilePath)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    /// <summary>把持久化文件中的 gain 覆盖到默认 configs（按行顺序），缺失/超长条目跳过。
    /// 优先读取用户导入的 %APPDATA% 文件；不存在时回退到随程序发行的 gainset.xlsx。</summary>
    public static void ApplyPersistedOverrides(List<WifiTestItem> configs)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    private static bool HasHeader(IXLWorksheet ws)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }
}
