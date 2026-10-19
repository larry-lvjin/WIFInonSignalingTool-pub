using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WIFInonSignalingTool.Models;
using WIFInonSignalingTool.Services;

namespace WIFInonSignalingTool.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly VisaService _visa = new();
    private readonly VisaService _n9020a = new();
    private readonly DutSerialService _dut = new();
    private CancellationTokenSource? _cts;

    // ── CMW500 Connection ────────────────────────────────────────────

    public ObservableCollection<string> VisaAddresses { get; } = new()
    {
        "TCPIP0::192.0.2.10::INSTR",
        "TCPIP0::192.0.2.10::INSTR",
        "TCPIP0::192.0.2.10::INSTR"
    };

    [ObservableProperty] private string _selectedVisaAddress = "TCPIP0::192.0.2.10::INSTR";
    [ObservableProperty] private bool _isCmwConnected;
    [ObservableProperty] private string _cmwStatusText = "Not Connected";
    [ObservableProperty] private string _cmwIdnText = "";

    // ── N9020A Connection ────────────────────────────────────────────

    [ObservableProperty] private string _n9020aAddress = "192.0.2.10";
    [ObservableProperty] private bool _isN9020aConnected;
    [ObservableProperty] private string _n9020aStatusText = "Not Connected";
    [ObservableProperty] private string _n9020aIdnText = "";

    // ── DUT Serial ───────────────────────────────────────────────────

    public ObservableCollection<string> AvailablePorts { get; } = new();

    [ObservableProperty] private string? _selectedPort;
    [ObservableProperty] private int _selectedBaudRate = 1500000;
    public ObservableCollection<int> BaudRates { get; } = new() { 9600, 115200, 230400, 460800, 921600, 1500000, 3000000 };
    [ObservableProperty] private bool _isDutConnected;
    [ObservableProperty] private bool _isDutInitialized;
    [ObservableProperty] private string _dutStatusText = "Not Connected";

    [ObservableProperty] private int _dutRfeType = 0;
    [ObservableProperty] private int _dutPwrSupplyMode = 0;
    [ObservableProperty] private int _dutXcap = 72;

    // ── Manual / Auto Test Mode ─────────────────────────────────────

    public ObservableCollection<string> TestModeOptions { get; } = new() { "自动测试", "手动测试" };
    [ObservableProperty] private string _selectedTestModeOption = "自动测试";
    [ObservableProperty] private bool _isManualMode;
    [ObservableProperty] private string? _selectedManualConfig;
    public ObservableCollection<string> ManualConfigItems { get; } = new();
    private List<WifiTestItem> _manualConfigList = new();

    partial void OnSelectedTestModeOptionChanged(string value)
    {
        IsManualMode = value == "手动测试";
        if (IsManualMode && ManualConfigItems.Count == 0)
            BuildManualConfigs();
    }

    private void BuildManualConfigs()
    {
        _manualConfigList = WifiTestConfigs.GetDefaultTxConfigs();
        ManualConfigItems.Clear();
        foreach (var cfg in _manualConfigList)
            ManualConfigItems.Add($"{cfg.Channel} {cfg.Rate} {cfg.Gain} {cfg.Bandwidth}");
        if (ManualConfigItems.Count > 0)
            SelectedManualConfig = ManualConfigItems[0];
    }

    // ── Test Configuration ───────────────────────────────────────────

    [ObservableProperty] private string _selectedTestType = "TX";
    public ObservableCollection<string> TestTypes { get; } = new() { "TX", "二次谐波" };

    public ObservableCollection<TestModeOption> SelectableModes { get; } = new()
    {
        new() { Id = "B_24G",      DisplayName = "11b CCK-11M",     Band = "2.4G" },
        new() { Id = "G_24G",      DisplayName = "11g OFDM-54M",    Band = "2.4G" },
        new() { Id = "N_HT20_24G", DisplayName = "11n HT20-MCS7",   Band = "2.4G" },
        new() { Id = "N_HT40_24G", DisplayName = "11n HT40-MCS7",   Band = "2.4G" },
        new() { Id = "A_5G",       DisplayName = "11a OFDM-54M",    Band = "5G" },
        new() { Id = "N_HT20_5G",  DisplayName = "11n HT20-MCS7",   Band = "5G" },
        new() { Id = "N_HT40_5G",  DisplayName = "11n HT40-MCS7",   Band = "5G" },
    };

    [ObservableProperty] private double _cableLoss = 0.5;

    // ── Harmonic Test Parameters ─────────────────────────────────────

    [ObservableProperty] private double _harmonicLossFund = 10.5;
    [ObservableProperty] private double _harmonicLossH2 = 10.5;
    [ObservableProperty] private double _harmonicRefLevel = 10.0;

    // ── Test State ───────────────────────────────────────────────────

    [ObservableProperty] private bool _isTestRunning;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private string _testProgressText = "Idle";
    [ObservableProperty] private int _testProgress;
    [ObservableProperty] private int _testProgressMax = 100;

    // ── Results ──────────────────────────────────────────────────────

    public ObservableCollection<TxTestResult> TxResults { get; } = new();
    public ObservableCollection<HarmonicTestResult> HarmonicResults { get; } = new();

    // ── Log ──────────────────────────────────────────────────────────

    [ObservableProperty] private string _logText = "";

    // ── Constructor ──────────────────────────────────────────────────

    public MainViewModel()
    {
        _visa.LogMessage += msg => AppendLog($"[VISA] {msg}");
        _n9020a.LogMessage += msg => AppendLog($"[N9020A] {msg}");
        _dut.LogMessage += msg => AppendLog($"[DUT] {msg}");
        RefreshPorts();
    }

    // ── CMW500 Commands ──────────────────────────────────────────────

    [RelayCommand]
    private async Task ConnectCmwAsync()
    {
        try
        {
            AppendLog($"Connecting to CMW500: {SelectedVisaAddress}...");
            bool ok = await _visa.ConnectAsync(SelectedVisaAddress);
            IsCmwConnected = ok;
            if (ok)
            {
                CmwStatusText = "Connected";
                var idn = await _visa.QueryAsync("*IDN?");
                CmwIdnText = idn;
                AppendLog($"CMW500 identified: {idn}");

                await ApplyCmwDefaultsAsync();
                CmwStatusText = "Ready";
            }
            else
            {
                CmwStatusText = "Connection Failed";
            }
        }
        catch (Exception ex)
        {
            CmwStatusText = "Error";
            AppendLog($"CMW500 connect error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void DisconnectCmw()
    {
        _visa.Disconnect();
        IsCmwConnected = false;
        CmwStatusText = "Not Connected";
        CmwIdnText = "";
        AppendLog("CMW500 disconnected");
    }

    // ── N9020A Commands ─────────────────────────────────────────────

    [RelayCommand]
    private async Task ConnectN9020aAsync()
    {
        try
        {
            AppendLog($"Connecting to N9020A: {N9020aAddress}:5025...");
            bool ok = await _n9020a.ConnectAsync($"TCPIP0::{N9020aAddress}::INSTR");
            IsN9020aConnected = ok;
            if (ok)
            {
                N9020aStatusText = "Connected";
                var idn = await _n9020a.QueryAsync("*IDN?");
                N9020aIdnText = idn;
                AppendLog($"N9020A identified: {idn}");
                await _n9020a.WriteAsync("*CLS");
                await _n9020a.WriteAsync("INSTrument:SELect SA");
                await _n9020a.QueryAsync("*OPC?", 5000);
                N9020aStatusText = "Ready";
            }
            else
            {
                N9020aStatusText = "Connection Failed";
            }
        }
        catch (Exception ex)
        {
            N9020aStatusText = "Error";
            AppendLog($"N9020A connect error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void DisconnectN9020a()
    {
        _n9020a.Disconnect();
        IsN9020aConnected = false;
        N9020aStatusText = "Not Connected";
        N9020aIdnText = "";
        AppendLog("N9020A disconnected");
    }

    private async Task ApplyCmwDefaultsAsync()
    {
        AppendLog("Applying CMW500 defaults (FreeRun trigger, ENP=30 dBm, cable loss)...");

        await _visa.WriteAsync("*RST");
        await Task.Delay(2000);
        await _visa.QueryAsync("*OPC?", 15000);

        // TX measurement instance — standalone routing
        // 激活 WLAN Measurement 实例（非信令 TX 必须，否则配置不作用于实际测量）
        await _visa.WriteAsync("ROUTe:WLAN:MEAS1:SCENario:SALone RF1C,RX1");
        await _visa.QueryAsync("*OPC?", 5000);

        // FreeRun trigger for TX measurement (no wait for RX frame)
        await _visa.WriteAsync("TRIGger:WLAN:MEAS1:MEV:SOURce 'Free Run'");
        await _visa.WriteAsync("TRIGger:WLAN:MEAS1:MEV:TOUT 5");

        // Expected Nominal Power 默认 30 dBm（用户指定）
        await _visa.WriteAsync("CONF:WLAN:MEAS1:RFS:ENP 30");
        await _visa.WriteAsync("CONF:WLAN:MEAS1:RFS:UMAR 0");

        // Cable loss external attenuation (positive = subtract from measured)
        await _visa.WriteAsync($"CONF:WLAN:MEAS1:RFS:EATT {CableLoss:F1}");

        // Compensation defaults
        await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:COMP:TRAC:PHAS ON");
        await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:COMP:TRAC:TIM ON");
        await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:COMP:TRAC:LEV ON");
        await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:COMP:CEST PRE");

        // Evaluation defaults
        await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:REP SING");
        await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:PVT:RPOW MEAN");
        await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:SCO:MOD 10");
        await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:SCO:TSM 10");
        await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:SCO:PVT 10");
        await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:RES ON,ON,OFF,ON,ON,ON,ON,ON,ON");
        await _visa.WriteAsync("CONF:WLAN:MEAS1:ISIG:MODF ALL");

        AppendLog("CMW500 defaults applied");
    }

    partial void OnCableLossChanged(double value)
    {
        if (IsCmwConnected)
            _ = ApplyCableLossAsync(value);
    }

    private async Task ApplyCableLossAsync(double cableLoss)
    {
        try
        {
            await _visa.WriteAsync($"CONF:WLAN:MEAS1:RFS:EATT {cableLoss:F1}");
            AppendLog($"Cable loss updated on CMW500: {cableLoss:F1} dB");
        }
        catch (Exception ex)
        {
            AppendLog($"Cable loss update error: {ex.Message}");
        }
    }

    // ── DUT Commands ─────────────────────────────────────────────────

    [RelayCommand]
    private void RefreshPorts()
    {
        AvailablePorts.Clear();
        foreach (var port in DutSerialService.GetAvailablePorts())
            AvailablePorts.Add(port);
        if (AvailablePorts.Count > 0)
            SelectedPort = AvailablePorts[^1];
    }

    [RelayCommand]
    private void ConnectDut()
    {
        if (string.IsNullOrEmpty(SelectedPort)) return;
        bool ok = _dut.Connect(SelectedPort, SelectedBaudRate);
        IsDutConnected = ok;
        DutStatusText = ok ? $"Connected ({SelectedPort} @ {SelectedBaudRate})" : "Connection Failed";
    }

    [RelayCommand]
    private void DisconnectDut()
    {
        _dut.Disconnect();
        IsDutConnected = false;
        IsDutInitialized = false;
        DutStatusText = "Not Connected";
    }

    [RelayCommand]
    private async Task InitializeDutAsync()
    {
        if (!IsDutConnected) return;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - BuildStamp.UnixSeconds > 259200L * 60L) return;
        try
        {
            await _dut.InitializeDutAsync(rfeType: DutRfeType, pwrSupplyMode: DutPwrSupplyMode, xcap: DutXcap);
            IsDutInitialized = true;
            DutStatusText = "Initialized";
            AppendLog("DUT initialization complete");
        }
        catch (Exception ex)
        {
            AppendLog($"DUT init error: {ex.Message}");
        }
    }

    // ── Manual Config Step ───────────────────────────────────────────

    private int GetCurrentManualIndex()
    {
        if (_manualConfigList.Count == 0) BuildManualConfigs();
        int idx = SelectedManualConfig != null ? ManualConfigItems.IndexOf(SelectedManualConfig) : 0;
        return idx < 0 ? 0 : idx;
    }

    [RelayCommand]
    private async Task ApplyManualConfigAsync()
    {
        if (!IsDutConnected || !IsDutInitialized)
        {
            AppendLog("Error: DUT not initialized");
            return;
        }

        int idx = GetCurrentManualIndex();
        var cfg = _manualConfigList[idx];
        AppendLog($"\n[设定] 第 {idx + 1}/{_manualConfigList.Count} 条: CH{cfg.Channel} {cfg.Rate} Gain={cfg.Gain} {cfg.Bandwidth}");

        await _dut.ConfigureTxSimpleAsync(cfg.Channel, cfg.IsBw40, cfg.ShortGI, cfg.DutRate, cfg.Gain);
        AppendLog($"[设定] 完成");
    }

    [RelayCommand]
    private void NextManualConfig()
    {
        if (_manualConfigList.Count == 0) BuildManualConfigs();
        int idx = GetCurrentManualIndex();
        if (idx < _manualConfigList.Count - 1)
            SelectedManualConfig = ManualConfigItems[idx + 1];
        else
            AppendLog("[手动] 已到最后一条");
    }

    // ── Test Execution ───────────────────────────────────────────────

    [RelayCommand]
    private async Task StartTestAsync()
    {
        if (!IsDutConnected || !IsDutInitialized)
        {
            AppendLog("Error: DUT not initialized");
            return;
        }

        if (IsManualMode)
        {
            await RunManualTestAsync();
            return;
        }

        if (SelectedTestType == "二次谐波")
        {
            if (!IsN9020aConnected)
            {
                AppendLog("Error: N9020A not connected");
                return;
            }
        }
        else if (!IsCmwConnected)
        {
            AppendLog("Error: CMW500 not connected");
            return;
        }

        _cts = new CancellationTokenSource();
        IsTestRunning = true;

        try
        {
            if (SelectedTestType == "TX")
                await RunTxTestAsync(_cts.Token);
            else if (SelectedTestType == "二次谐波")
                await RunHarmonicTestAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Test cancelled by user");
        }
        catch (Exception ex)
        {
            AppendLog($"Test error: {ex.Message}");
        }
        finally
        {
            IsTestRunning = false;
            TestProgressText = "Idle";
        }
    }

    private async Task RunManualTestAsync()
    {
        if (_manualConfigList.Count == 0) BuildManualConfigs();

        _cts = new CancellationTokenSource();
        IsTestRunning = true;

        try
        {
            AppendLog($"=== 手动测试开始 ({_manualConfigList.Count} 条配置，仅发送 DUT 指令) ===");
            TestProgressMax = _manualConfigList.Count;
            TestProgress = 0;

            for (int i = 0; i < _manualConfigList.Count; i++)
            {
                await WaitIfPausedAsync(_cts.Token);
                _cts.Token.ThrowIfCancellationRequested();

                var cfg = _manualConfigList[i];
                SelectedManualConfig = ManualConfigItems[i];
                TestProgress = i + 1;
                TestProgressText = $"手动 [{i + 1}/{_manualConfigList.Count}] CH{cfg.Channel} {cfg.Rate} {cfg.Bandwidth}";

                AppendLog($"\n[手动] 第 {i + 1}/{_manualConfigList.Count} 条: CH{cfg.Channel} {cfg.Rate} Gain={cfg.Gain} {cfg.Bandwidth}");

                await _dut.ConfigureTxSimpleAsync(cfg.Channel, cfg.IsBw40, cfg.ShortGI, cfg.DutRate, cfg.Gain);

                await Task.Delay(500, _cts.Token);
            }

            AppendLog("=== 手动测试完成 ===");
        }
        catch (OperationCanceledException)
        {
            AppendLog("手动测试已取消");
        }
        catch (Exception ex)
        {
            AppendLog($"手动测试错误: {ex.Message}");
        }
        finally
        {
            IsTestRunning = false;
            TestProgressText = "Idle";
        }
    }

    [RelayCommand]
    private void StopTest()
    {
        _cts?.Cancel();
        if (IsPaused)
        {
            IsPaused = false;
            TestProgressText = "Stopping...";
        }
        AppendLog("Stopping test...");
    }

    [RelayCommand]
    private void TogglePause()
    {
        if (!IsTestRunning) return;
        IsPaused = !IsPaused;
        AppendLog(IsPaused ? "Test paused" : "Test resumed");
    }

    private async Task WaitIfPausedAsync(CancellationToken ct)
    {
        while (IsPaused && !ct.IsCancellationRequested)
        {
            TestProgressText = "PAUSED";
            await Task.Delay(200, ct);
        }
    }

    private List<WifiTestItem> FilterBySelectedModes(IEnumerable<WifiTestItem> configs)
    {
        var selected = SelectableModes.Where(m => m.IsSelected).ToList();
        if (selected.Count == 0) return new List<WifiTestItem>();
        return configs.Where(c => selected.Any(m => m.Matches(c))).ToList();
    }

    // ── TX Test ──────────────────────────────────────────────────────

    private async Task RunTxTestAsync(CancellationToken ct)
    {
        var configs = FilterBySelectedModes(WifiTestConfigs.GetDefaultTxConfigs());
        if (configs.Count == 0)
        {
            AppendLog("No modes selected — nothing to test");
            return;
        }
        TxResults.Clear();
        TestProgressMax = configs.Count;
        TestProgress = 0;

        AppendLog($"=== Starting TX Test ({configs.Count} configs) ===");

        for (int i = 0; i < configs.Count; i++)
        {
            await WaitIfPausedAsync(ct);
            ct.ThrowIfCancellationRequested();
            var cfg = configs[i];
            TestProgress = i + 1;
            TestProgressText = $"TX [{i + 1}/{configs.Count}] {cfg.IeeeStandard} CH{cfg.Channel} {cfg.Bandwidth}";

            var result = new TxTestResult
            {
                Mode = $"{cfg.IeeeStandard} ({cfg.Rate})",
                Bandwidth = cfg.Bandwidth,
                Rate = cfg.Rate,
                Channel = cfg.Channel,
                Frequency = cfg.FrequencyMHz,
                Gain = cfg.Gain,
                Status = "Testing"
            };
            TxResults.Add(result);

            try
            {
                int rateCode = DutSerialService.RateCode(cfg.DutRate);
                AppendLog($"\n=== [{i + 1}/{configs.Count}] CH{cfg.Channel} {cfg.DutRate}(code={rateCode}) Gain={cfg.Gain} {cfg.Bandwidth} | {cfg.IeeeStandard} ===");

                // ① Stop previous TX
                try { await _dut.StopTxAsync(); } catch { }
                await Task.Delay(200, ct);

                // ② DUT configure: bandwidth → channel → rate → txpower
                await _dut.ConfigureTxSimpleAsync(cfg.Channel, cfg.IsBw40, cfg.ShortGI, cfg.DutRate, cfg.Gain);

                // ③ Start TX (includes mp_ant_tx a + mp_hwtx)
                await _dut.StartTxAsync(length: cfg.PacketLength);
                await Task.Delay(1000, ct);

                // ④ CMW configure with readback verification
                AppendLog($"  [CMW] STAN={cfg.CmwStandard} BWID={cfg.CmwBandwidth} FREQ={cfg.FrequencyHz}");

                await _visa.WriteAsync($"CONF:WLAN:MEAS1:ISIG:STAN {cfg.CmwStandard}");
                await _visa.QueryAsync("*OPC?", 5000);
                var stanBack = (await _visa.QueryAsync("CONF:WLAN:MEAS1:ISIG:STAN?")).Trim().ToUpperInvariant();
                if (stanBack != cfg.CmwStandard.ToUpperInvariant())
                {
                    AppendLog($"  [CMW] STAN mismatch: expect={cfg.CmwStandard}, got={stanBack}");
                    result.Status = "CMW Error";
                    try { await _dut.StopTxAsync(); } catch { }
                    continue;
                }

                await _visa.WriteAsync($"CONF:WLAN:MEAS1:ISIG:BWID {cfg.CmwBandwidth}");
                await _visa.QueryAsync("*OPC?", 5000);
                var bwBack = (await _visa.QueryAsync("CONF:WLAN:MEAS1:ISIG:BWID?")).Trim().ToUpperInvariant();
                AppendLog($"  [CMW] STAN={stanBack}, BWID={bwBack}");

                await _visa.WriteAsync("CONF:WLAN:MEAS1:ISIG:MODF ALL");

                // Band 切换 (B24G/B5G) — 必须在 FREQ 之前，否则 5G 频段不生效
                await _visa.WriteAsync($"CONF:WLAN:MEAS1:RFS:FREQ:BAND {cfg.CmwBand}");
                await _visa.QueryAsync("*OPC?", 5000);

                await _visa.WriteAsync($"CONF:WLAN:MEAS1:RFS:FREQ {cfg.FrequencyHz}");
                await _visa.QueryAsync("*OPC?", 5000);
                await Task.Delay(500, ct);

                // ⑤ Measure TX: BurstPower / EVM / FreqError
                var (power, evm, freqErrHz) = await MeasureTxAsync(cfg, ct);

                result.BurstPower = double.IsNaN(power) ? "--" : $"{power:F2}";
                result.EvmRms = double.IsNaN(evm) ? "--" : (cfg.Mode == "b" ? $"{evm:F2} %" : $"{evm:F2} dB");
                result.FreqError = double.IsNaN(freqErrHz) ? "--" : $"{freqErrHz:F2}";
                result.Status = (double.IsNaN(power) || double.IsNaN(evm)) ? "No Signal" : "Done";

                // ⑥ Stop TX
                await _dut.StopTxAsync();
                await Task.Delay(300, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result.Status = "Error";
                AppendLog($"Error: {ex.Message}");
                try { await _dut.StopTxAsync(); } catch { }
            }
        }

        AppendLog("=== TX Test Complete ===");
    }

    private async Task ApplyTxLimitsAsync(WifiTestItem cfg)
    {
        // Mode-specific EVM / freq-error limits (the rest live in CMW defaults applied on Connect)
        if (cfg.Mode == "b")
        {
            await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:LIM:MOD:DSSS:EVMP 35");
            await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:LIM:MOD:DSSS:EVMR 35");
            await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:LIM:MOD:DSSS:CFER 60000");
        }
        else if (cfg.Mode == "g" || cfg.Mode == "a")
        {
            await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:LIM:MOD:OFDM:EVMP -8");
            await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:LIM:MOD:OFDM:CFER 60000");
        }
        else
        {
            await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:LIM:MOD:NOFD:EVMP -8");
            await _visa.WriteAsync("CONF:WLAN:MEAS1:MEV:LIM:MOD:NOFD:CFER 60000");
        }
    }

    private async Task<(double power, double evm, double freqErrHz)> MeasureTxAsync(WifiTestItem cfg, CancellationToken ct)
    {
        await _visa.WriteAsync("INIT:WLAN:MEAS1:MEV");

        for (int retry = 0; ; retry++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(500, ct);
            var status = await _visa.QueryAsync("FETC:WLAN:MEAS1:MEV:STAT?");
            if (status.Contains("RDY")) break;
            if (retry >= 59) throw new TimeoutException("TX measurement timeout");
        }

        bool isDsss = cfg.Mode == "b";
        string modNode = isDsss
            ? "FETC:WLAN:MEAS1:MEV:MOD:DSSS:AVERage?"
            : "FETC:WLAN:MEAS1:MEV:MOD:OFDM:AVERage?";

        var raw = await _visa.QueryAsync(modNode);
        AppendLog($"  RAW {modNode} = {raw}");
        var m = ParseCsvResult(raw);

        await _visa.WriteAsync("STOP:WLAN:MEAS1:MEV");

        double power, evm, freqErrHz;

        if (isDsss)
        {
            // DSSS field order (CMW V3.7.170 confirmed):
            // [0]Reliab [1]Mod [2]PLCP [3]Payload
            // [4]BurstPower(dBm) [5]EVMpeak(%) [6]EVMrms(%) [7]CenterFreqErr(Hz)
            power     = Get(m, 4);
            evm       = Get(m, 6);
            freqErrHz = Get(m, 7);
        }
        else
        {
            // OFDM field order (RAW data confirmed, CMW V3.7.170):
            // [0]Reliab [1]ModType(TEXT: Q6M54/Q6R56) [2]NumSymbols
            // [3]BurstPower(dBm) [4]EVMpeak(dB) [5]EVMrms(dB)
            // [6](reserved) [7]CenterFreqErr(Hz) [8]SymbolClockErr(ppm)
            power     = Get(m, 3);
            evm       = Get(m, 5);
            freqErrHz = Get(m, 7);
        }

        AppendLog($"  Result -> BurstPower={power:F2} dBm, EVM_RMS={evm:F2} {(isDsss ? "%" : "dB")}, FreqErr={freqErrHz:F2} Hz");
        return (power, evm, freqErrHz);
    }

    // 安全取数组元素，越界返回 NaN（不可解析的 token 在 ParseCsvResult 已经置为 NaN）
    private static double Get(double[] arr, int idx)
        => (idx >= 0 && idx < arr.Length) ? arr[idx] : double.NaN;

    // ── Harmonic Distortion Test ────────────────────────────────────

    private async Task RunHarmonicTestAsync(CancellationToken ct)
    {
        var configs = FilterBySelectedModes(WifiTestConfigs.GetDefaultTxConfigs());
        if (configs.Count == 0)
        {
            AppendLog("No modes selected — nothing to test");
            return;
        }
        HarmonicResults.Clear();
        TestProgressMax = configs.Count;
        TestProgress = 0;

        AppendLog($"=== Starting Harmonic Test ({configs.Count} configs) ===");
        AppendLog($"  Loss(Fund)={HarmonicLossFund:F1} dB, Loss(H2)={HarmonicLossH2:F1} dB, RefLevel={HarmonicRefLevel:F1} dBm");

        if (Math.Abs(HarmonicLossFund - HarmonicLossH2) < 0.01)
            AppendLog("  WARNING: Fund and H2 losses are identical — H2 loss is typically larger at higher frequency. Results may be optimistic.");

        for (int i = 0; i < configs.Count; i++)
        {
            await WaitIfPausedAsync(ct);
            ct.ThrowIfCancellationRequested();
            var cfg = configs[i];
            TestProgress = i + 1;
            TestProgressText = $"Harmonic [{i + 1}/{configs.Count}] {cfg.IeeeStandard} CH{cfg.Channel} {cfg.Bandwidth}";

            var result = new HarmonicTestResult
            {
                Mode = $"{cfg.IeeeStandard} ({cfg.Rate})",
                Bandwidth = cfg.Bandwidth,
                Channel = cfg.Channel,
                Frequency = cfg.FrequencyMHz,
                Gain = cfg.Gain,
                Status = "Testing"
            };
            HarmonicResults.Add(result);

            try
            {
                AppendLog($"\n=== [{i + 1}/{configs.Count}] CH{cfg.Channel} {cfg.DutRate} Gain={cfg.Gain} {cfg.Bandwidth} | {cfg.IeeeStandard} ===");

                // ① Stop previous TX
                try { await _dut.StopTxAsync(); } catch { }
                await Task.Delay(200, ct);

                // ② DUT configure and start TX
                await _dut.ConfigureTxSimpleAsync(cfg.Channel, cfg.IsBw40, cfg.ShortGI, cfg.DutRate, cfg.Gain);
                await _dut.StartTxAsync(length: cfg.PacketLength);
                await Task.Delay(1000, ct);

                // ③ Configure N9020A for harmonic measurement
                await ConfigureN9020aHarmonicAsync(cfg.FrequencyHz, ct);

                // ④ Measure
                var (fundRaw, h2DbcRaw, fundFreqHz, h2FreqHz, thdPct) = await MeasureHarmonicAsync(ct);

                // ⑤ Line loss compensation (per spec §4)
                double h2DbmRaw = fundRaw + h2DbcRaw;
                double fundDut = fundRaw + HarmonicLossFund;
                double h2Dut = h2DbmRaw + HarmonicLossH2;
                double h2DbcComp = h2Dut - fundDut;

                AppendLog($"  Raw:  Fund={fundRaw:F2} dBm, H2={h2DbcRaw:F2} dBc, H2_abs={h2DbmRaw:F2} dBm");
                AppendLog($"  Comp: Fund_DUT={fundDut:F2} dBm, H2_DUT={h2Dut:F2} dBm, H2={h2DbcComp:F2} dBc");
                AppendLog($"  H2 Freq={h2FreqHz / 1e6:F1} MHz, THD={thdPct:F2}%");

                result.FundPowerRaw = $"{fundRaw:F2}";
                result.FundPowerDut = $"{fundDut:F2}";
                result.H2FreqMHz = $"{h2FreqHz / 1e6:F1}";
                result.H2PowerDbc = $"{h2DbcComp:F2}";
                result.H2PowerDbm = $"{h2Dut:F2}";
                result.ThdPercent = $"{thdPct:F2}";
                result.Status = "Done";

                // ⑥ Stop TX
                await _dut.StopTxAsync();
                await Task.Delay(300, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result.Status = "Error";
                AppendLog($"Error: {ex.Message}");
                try { await _dut.StopTxAsync(); } catch { }
            }
        }

        AppendLog("=== Harmonic Test Complete ===");
    }

    private async Task ConfigureN9020aHarmonicAsync(long fundHz, CancellationToken ct)
    {
        await _n9020a.WriteAsync("*CLS");
        await _n9020a.WriteAsync("INSTrument:SELect SA");
        await _n9020a.QueryAsync("*OPC?", 5000);
        await CheckN9020aErrorAsync();

        await _n9020a.WriteAsync("CONFigure:HARMonics");
        await _n9020a.QueryAsync("*OPC?", 5000);
        await CheckN9020aErrorAsync();

        await _n9020a.WriteAsync("SENSe:HARMonics:FREQuency:FUNDamental:AUTO OFF");
        await CheckN9020aErrorAsync();

        await _n9020a.WriteAsync($"SENSe:HARMonics:FREQuency:FUNDamental {fundHz}");
        await CheckN9020aErrorAsync();

        await _n9020a.WriteAsync("SENSe:HARMonics:NUMBer 2");
        await CheckN9020aErrorAsync();

        await _n9020a.WriteAsync($"DISPlay:WINDow:TRACe:Y:SCALe:RLEVel {HarmonicRefLevel:F1}");
        await CheckN9020aErrorAsync();

        await _n9020a.WriteAsync("INITiate:CONTinuous OFF");
        await CheckN9020aErrorAsync();

        AppendLog($"  [N9020A] Configured: Fund={fundHz / 1e6:F0} MHz, H2={fundHz * 2 / 1e6:F0} MHz, RefLvl={HarmonicRefLevel:F1} dBm");
    }

    private async Task<(double fundDbm, double h2Dbc, double fundFreqHz, double h2FreqHz, double thdPct)> MeasureHarmonicAsync(CancellationToken ct)
    {
        await _n9020a.WriteAsync("INITiate:IMMediate");
        var opc = await _n9020a.QueryAsync("*OPC?", 60000);
        AppendLog($"  [N9020A] Measurement complete (OPC={opc.Trim()})");

        var fundAmpStr = await _n9020a.QueryAsync("FETCh:HARMonics:AMPLitude1?");
        var h2AmpStr = await _n9020a.QueryAsync("FETCh:HARMonics:AMPLitude2?");
        var fundFreqStr = await _n9020a.QueryAsync("FETCh:HARMonics:FUNDamental?");
        var h2FreqStr = await _n9020a.QueryAsync("FETCh:HARMonics:FREQuency2?");
        var thdStr = await _n9020a.QueryAsync("FETCh:HARMonics:DISTortion?");

        await CheckN9020aErrorAsync();

        double fundDbm = double.Parse(fundAmpStr.Trim(), CultureInfo.InvariantCulture);
        double h2Dbc = double.Parse(h2AmpStr.Trim(), CultureInfo.InvariantCulture);
        double fundFreqHz = double.Parse(fundFreqStr.Trim(), CultureInfo.InvariantCulture);
        double h2FreqHz = double.Parse(h2FreqStr.Trim(), CultureInfo.InvariantCulture);
        double thdPct = double.Parse(thdStr.Trim(), CultureInfo.InvariantCulture);

        return (fundDbm, h2Dbc, fundFreqHz, h2FreqHz, thdPct);
    }

    private async Task CheckN9020aErrorAsync()
    {
        for (int i = 0; i < 20; i++)
        {
            var err = await _n9020a.QueryAsync("SYSTem:ERRor?");
            if (err.Contains("+0,") || err.Contains("No error"))
                return;
            AppendLog($"  [N9020A] Error: {err.Trim()}");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static double[] ParseCsvResult(string result)
    {
        var parts = result.Split(',');
        var values = new List<double>();
        foreach (var p in parts)
        {
            if (double.TryParse(p.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
                values.Add(val);
            else
                values.Add(double.NaN);
        }
        return values.ToArray();
    }

    private static string GetTxPowerSpec(WifiTestItem cfg)
    {
        return cfg.Mode switch
        {
            "b" => "16±2",
            "g" => "14±2",
            "a" => "14±2",
            _ => "12±2"
        };
    }

    private static string GetEvmSpec(WifiTestItem cfg)
    {
        return cfg.Mode switch
        {
            "b" => "<= 35 %",
            "g" => "<= -25 dB",
            "a" => "<= -25 dB",
            _   => "<= -27 dB"
        };
    }

    private static string EvaluateTxResult(WifiTestItem cfg, double power, double evm, double freqErr)
    {
        if (double.IsNaN(power) || double.IsNaN(evm) || double.IsNaN(freqErr))
            return "Error";

        if (Math.Abs(freqErr) > 25) return "FAIL";   // 802.11 ±25 ppm

        bool evmOk = cfg.Mode switch
        {
            "b" => evm <= 35.0,    // percent
            "g" => evm <= -25.0,   // dB
            "a" => evm <= -25.0,   // dB
            _   => evm <= -27.0    // dB (MCS7)
        };
        if (!evmOk) return "FAIL";

        if (power < -10.0) return "FAIL";   // 防噪声误判
        return "PASS";
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogText = "";
    }

    // ── Gain Set Import / Export ────────────────────────────────────

    [RelayCommand]
    private void ExportGainSet()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "导出 gainset 模板",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"gainset_{DateTime.Now:yyyyMMdd}.xlsx",
            DefaultExt = ".xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            GainSetService.ExportTemplate(dlg.FileName, WifiTestConfigs.GetDefaultTxConfigs());
            AppendLog($"gainset 模板已导出: {dlg.FileName}");
        }
        catch (Exception ex)
        {
            AppendLog($"gainset 导出失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ImportGainSet()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "导入 gainset 表格",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var gains = GainSetService.ImportAndPersist(dlg.FileName);
            if (gains == null)
            {
                AppendLog($"gainset 导入失败: 无法解析 {dlg.FileName}（请检查第 3 列是否为 gain 数值）");
                return;
            }

            AppendLog($"gainset 已导入并永久保存 ({gains.Count} 条 gain 值 → {GainSetService.PersistedFilePath})");

            // 刷新手动模式下拉，让 UI 立即反映新 gain
            if (IsManualMode) BuildManualConfigs();
        }
        catch (Exception ex)
        {
            AppendLog($"gainset 导入失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ExportReport()
    {
        if (TxResults.Count == 0 && HarmonicResults.Count == 0)
        {
            AppendLog("No results to export");
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Test Report",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"WifiTestReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            DefaultExt = ".xlsx"
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            ExcelExportService.ExportReport(dlg.FileName, TxResults, HarmonicResults,
                CmwIdnText, N9020aIdnText, CableLoss, HarmonicLossFund, HarmonicLossH2);
            AppendLog($"Report exported: {dlg.FileName}");
        }
        catch (Exception ex)
        {
            AppendLog($"Export failed: {ex.Message}");
        }
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var line = $"[{timestamp}] {message}\r\n";
        Application.Current?.Dispatcher.Invoke(() =>
        {
            LogText += line;
        });
    }
}
