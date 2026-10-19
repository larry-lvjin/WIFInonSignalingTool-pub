using CommunityToolkit.Mvvm.ComponentModel;

namespace WIFInonSignalingTool.Models;

public partial class TxTestResult : ObservableObject
{
    [ObservableProperty] private string _mode = "";
    [ObservableProperty] private string _bandwidth = "";
    [ObservableProperty] private string _rate = "";
    [ObservableProperty] private int _channel;
    [ObservableProperty] private string _frequency = "";
    [ObservableProperty] private int _gain;
    [ObservableProperty] private string _burstPower = "--";       // dBm
    [ObservableProperty] private string _evmRms = "--";           // 11b:% / OFDM:dB
    [ObservableProperty] private string _freqError = "--";        // ppm
    [ObservableProperty] private string _status = "Pending";      // Testing/Done/Error
}

public partial class HarmonicTestResult : ObservableObject
{
    [ObservableProperty] private string _mode = "";
    [ObservableProperty] private string _bandwidth = "";
    [ObservableProperty] private int _channel;
    [ObservableProperty] private string _frequency = "";
    [ObservableProperty] private int _gain;
    [ObservableProperty] private string _fundPowerRaw = "--";
    [ObservableProperty] private string _fundPowerDut = "--";
    [ObservableProperty] private string _h2FreqMHz = "--";
    [ObservableProperty] private string _h2PowerDbc = "--";
    [ObservableProperty] private string _h2PowerDbm = "--";
    [ObservableProperty] private string _thdPercent = "--";
    [ObservableProperty] private string _status = "Pending";
}

public partial class TestModeOption : ObservableObject
{
    [ObservableProperty] private bool _isSelected = true;
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Band { get; init; } = "";

    public bool Matches(WifiTestItem cfg)
    {
        bool is24G = cfg.FrequencyHz < 4_000_000_000;
        bool is40M = cfg.Bandwidth == "40M";

        return Id switch
        {
            "B_24G"      => cfg.Mode == "b" && is24G,
            "G_24G"      => cfg.Mode == "g" && is24G,
            "N_HT20_24G" => cfg.Mode == "n" && !is40M && is24G,
            "N_HT40_24G" => cfg.Mode == "n" && is40M && is24G,
            "A_5G"       => cfg.Mode == "a" && !is24G,
            "N_HT20_5G"  => cfg.Mode == "n" && !is40M && !is24G,
            "N_HT40_5G"  => cfg.Mode == "n" && is40M && !is24G,
            _ => false
        };
    }
}
