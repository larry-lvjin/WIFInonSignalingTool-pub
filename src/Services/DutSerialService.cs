using System.IO.Ports;
using System.Text;

namespace WIFInonSignalingTool.Services;

public class DutSerialService : IDisposable
{
    private SerialPort? _port;
    private bool _disposed;

    public bool IsConnected => _port?.IsOpen ?? false;
    public string PortName => _port?.PortName ?? "";
    public event Action<string>? LogMessage;

    public static string[] GetAvailablePorts() => SerialPort.GetPortNames();
    public static string? AutoDetectPort()
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    public bool Connect(string portName, int baudRate = 115200)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    public void Disconnect()
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    public string SendCommand(string command, int delayMs = 400)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    public async Task<string> SendCommandAsync(string command, int delayMs = 400)
        => await Task.Run(() => SendCommand(command, delayMs));

    public static int RateCode(string rate) => rate.ToUpperInvariant() switch
    {
        "CCK_1M" => 2, "CCK_2M" => 4, "CCK_5.5M" => 11, "CCK_11M" => 22,
        "OFDM_6M" => 12, "OFDM_9M" => 18, "OFDM_12M" => 24, "OFDM_18M" => 36,
        "OFDM_24M" => 48, "OFDM_36M" => 72, "OFDM_48M" => 96, "OFDM_54M" => 108,
        "MCS0" => 128, "MCS1" => 129, "MCS2" => 130, "MCS3" => 131,
        "MCS4" => 132, "MCS5" => 133, "MCS6" => 134, "MCS7" => 135,
        "MCS8" => 136, "MCS9" => 137, "MCS10" => 138, "MCS11" => 139,
        "MCS12" => 140, "MCS13" => 141, "MCS14" => 142, "MCS15" => 143,
        _ => 22
    };

    public async Task InitializeDutAsync(int rfeType = 0, int pwrSupplyMode = 0, int xcap = 72)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    public async Task ConfigureTxSimpleAsync(int channel, bool bw40, bool shortGI, string rate, int gain)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    public async Task ConfigureTxAsync(int channel, bool bw40, bool shortGI, string rate, int gain)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    public async Task StartTxAsync(int periodUs = 1000, int length = 1500)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    public async Task StopTxAsync()
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    /// <summary>
    /// 配置 TX 并逐条校验样机回复。返回每项是否确认成功。
    /// 样机回复格式（实测）:
    ///   mp_channel 3        -> "Change channel X to channel 3"
    ///   mp_rate 4           -> "Set data rate to 4"
    ///   mp_txpower patha=89 -> "Set power level path_A:89 path_B:0"
    ///   mp_hwtx ...         -> "Start HW packet TX period=..."
    /// </summary>
    public async Task<(bool ok, string detail)> ConfigureTxVerifiedAsync(
        int channel, bool bw40, bool shortGI, string rate, int gain, int length = 1500)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    private static string CleanEcho(string raw)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    private void Log(string msg) => LogMessage?.Invoke(msg);

    public void Dispose()
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }
}
