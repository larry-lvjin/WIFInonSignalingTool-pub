using WIFInonSignalingTool.Services;

namespace WIFInonSignalingTool.Models;

public class WifiTestItem
{
    public string Mode { get; set; } = "";
    public string Standard { get; set; } = "";
    public string Bandwidth { get; set; } = "20M";
    public string Rate { get; set; } = "";
    public int Channel { get; set; }
    public long FrequencyHz { get; set; }
    public int Gain { get; set; }

    public int PacketLength { get; set; } = 1000;

    public string FrequencyMHz => $"{FrequencyHz / 1_000_000}";
    public string DisplayName => $"{Mode} CH{Channel}";
    public string BandTag => FrequencyHz < 4_000_000_000 ? "2.4G" : "5G";

    // CMW measurement standard (CONF:WLAN:MEAS1:ISIG:STAN)
    // 实测 CMW V3.7.170 接受的合法值（SCPI 探测确认，回读一致）：
    //   DSSS=802.11b  LOFD=802.11a/g legacy OFDM  HTOF=802.11n HT  VHT=802.11ac
    public string CmwStandard => Mode switch
    {
        "b" => "DSSS",
        "g" => "LOFD",
        "a" => "LOFD",
        "n" => "HTOF",
        _   => "LOFD"
    };

    public string CmwBandwidth => Bandwidth switch
    {
        "40M" => "BW40",
        _ => "BW20"
    };

    // Standard label used in step-by-step logging
    public string IeeeStandard => Mode switch
    {
        "b" => "IEEE 802.11b",
        "g" => "IEEE 802.11g",
        "a" => "IEEE 802.11a/n",
        "n" when FrequencyHz >= 4_000_000_000 => "IEEE 802.11a/n",
        "n" when Bandwidth == "40M"            => "IEEE 802.11a/n",
        _   => "IEEE 802.11g/n"
    };

    // DUT rate label fed to DutSerialService.RateCode(); normalize hyphen->underscore
    public string DutRate => (string.IsNullOrWhiteSpace(Rate) ? Mode switch
    {
        "b" => "CCK_11M",
        "g" => "OFDM_54M",
        "a" => "OFDM_54M",
        _   => "MCS7"
    } : Rate).Replace("-", "_").ToUpperInvariant();

    // CMW500 频段选择 (CONF:WLAN:MEAS1:RFS:FREQ:BAND)
    public string CmwBand => FrequencyHz >= 4_000_000_000 ? "B5G" : "B24G";

    public bool IsBw40 => Bandwidth == "40M";
    public bool ShortGI => false;
}

public static class WifiTestConfigs
{
    private static int PacketLengthFor(string mode) => mode switch
    {
        "b" => 1024,  // 802.11b CCK
        "g" => 1000,  // 802.11g OFDM
        "a" => 1000,  // 802.11a OFDM
        _   => 4096   // 802.11n HT20/HT40
    };

    private static WifiTestItem ApplyStandards(WifiTestItem item)
    {
        item.PacketLength = PacketLengthFor(item.Mode);
        return item;
    }

    public static List<WifiTestItem> GetDefaultTxConfigs()
    {
        var list = new List<WifiTestItem>
        {
            // 2.4G CCK (11b)
            new() { Mode = "b", Standard = "CCK_11M", Bandwidth = "20M", Rate = "CCK_11M", Channel = 3, FrequencyHz = 2_422_000_000, Gain = 89 },
            new() { Mode = "b", Standard = "CCK_11M", Bandwidth = "20M", Rate = "CCK_11M", Channel = 7, FrequencyHz = 2_442_000_000, Gain = 91 },
            new() { Mode = "b", Standard = "CCK_11M", Bandwidth = "20M", Rate = "CCK_11M", Channel = 11, FrequencyHz = 2_462_000_000, Gain = 91 },
            // 2.4G OFDM (11g)
            new() { Mode = "g", Standard = "OFDM_54M", Bandwidth = "20M", Rate = "OFDM_54M", Channel = 3, FrequencyHz = 2_422_000_000, Gain = 82 },
            new() { Mode = "g", Standard = "OFDM_54M", Bandwidth = "20M", Rate = "OFDM_54M", Channel = 7, FrequencyHz = 2_442_000_000, Gain = 83 },
            new() { Mode = "g", Standard = "OFDM_54M", Bandwidth = "20M", Rate = "OFDM_54M", Channel = 13, FrequencyHz = 2_472_000_000, Gain = 82 },
            // 2.4G HT20 (11n)
            new() { Mode = "n", Standard = "MCS7", Bandwidth = "20M", Rate = "MCS7", Channel = 3, FrequencyHz = 2_422_000_000, Gain = 78 },
            new() { Mode = "n", Standard = "MCS7", Bandwidth = "20M", Rate = "MCS7", Channel = 7, FrequencyHz = 2_442_000_000, Gain = 79 },
            new() { Mode = "n", Standard = "MCS7", Bandwidth = "20M", Rate = "MCS7", Channel = 13, FrequencyHz = 2_472_000_000, Gain = 78 },
            // 2.4G HT40 (11n)
            new() { Mode = "n", Standard = "MCS7", Bandwidth = "40M", Rate = "MCS7", Channel = 3, FrequencyHz = 2_422_000_000, Gain = 78 },
            new() { Mode = "n", Standard = "MCS7", Bandwidth = "40M", Rate = "MCS7", Channel = 7, FrequencyHz = 2_442_000_000, Gain = 79 },
            new() { Mode = "n", Standard = "MCS7", Bandwidth = "40M", Rate = "MCS7", Channel = 11, FrequencyHz = 2_462_000_000, Gain = 78 },
            // 5G OFDM (11a)
            new() { Mode = "a", Standard = "OFDM_54M", Bandwidth = "20M", Rate = "OFDM_54M", Channel = 36, FrequencyHz = 5_180_000_000, Gain = 100 },
            new() { Mode = "a", Standard = "OFDM_54M", Bandwidth = "20M", Rate = "OFDM_54M", Channel = 100, FrequencyHz = 5_500_000_000, Gain = 83 },
            new() { Mode = "a", Standard = "OFDM_54M", Bandwidth = "20M", Rate = "OFDM_54M", Channel = 149, FrequencyHz = 5_745_000_000, Gain = 74 },
            // 5G HT20 (11n)
            new() { Mode = "n", Standard = "MCS7", Bandwidth = "20M", Rate = "MCS7", Channel = 36, FrequencyHz = 5_180_000_000, Gain = 96 },
            new() { Mode = "n", Standard = "MCS7", Bandwidth = "20M", Rate = "MCS7", Channel = 100, FrequencyHz = 5_500_000_000, Gain = 79 },
            new() { Mode = "n", Standard = "MCS7", Bandwidth = "20M", Rate = "MCS7", Channel = 149, FrequencyHz = 5_745_000_000, Gain = 70 },
            // 5G HT40 (11n)
            new() { Mode = "n", Standard = "MCS7", Bandwidth = "40M", Rate = "MCS7", Channel = 38, FrequencyHz = 5_190_000_000, Gain = 96 },
            new() { Mode = "n", Standard = "MCS7", Bandwidth = "40M", Rate = "MCS7", Channel = 102, FrequencyHz = 5_510_000_000, Gain = 79 },
            new() { Mode = "n", Standard = "MCS7", Bandwidth = "40M", Rate = "MCS7", Channel = 159, FrequencyHz = 5_795_000_000, Gain = 70 },
        };
        GainSetService.ApplyPersistedOverrides(list);
        return list.Select(ApplyStandards).ToList();
    }
}
