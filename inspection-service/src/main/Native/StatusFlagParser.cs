using System.Runtime.InteropServices;

namespace FactoryPulse.Inspection.Native;

/// <summary>
/// P/Invoke wrapper for the native C status flag parser (libstatusflag.so).
/// Build the .so first: gcc -shared -fPIC -O2 -o libstatusflag.so status_flags.c
/// </summary>
public static class StatusFlagParser
{
    private const string LibName = "libstatusflag";

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeStatusFlags
    {
        public int PrimaryStatus;   // 0=Online, 1=Offline, 2=Maintenance, 3=Fault
        public int Overheating;
        public int Vibration;
        public int PressureAlert;
        public int LowLubricant;
        public int FaultCode;
        public int AlertCount;
    }

    [DllImport(LibName, EntryPoint = "parse_status_flags")]
    private static extern int ParseStatusFlagsNative(uint raw, out NativeStatusFlags result);

    [DllImport(LibName, EntryPoint = "encode_status_flags")]
    private static extern uint EncodeStatusFlagsNative(ref NativeStatusFlags flags);

    public static ParsedFlags Parse(int rawFlags)
    {
        try
        {
            int rc = ParseStatusFlagsNative((uint)rawFlags, out var native);
            if (rc != 0) return ParsedFlags.Default;

            return new ParsedFlags
            {
                PrimaryStatus = native.PrimaryStatus switch
                {
                    0 => "Online",
                    1 => "Offline",
                    2 => "Maintenance",
                    3 => "Fault",
                    _ => "Unknown"
                },
                HasOverheating    = native.Overheating    != 0,
                HasVibration      = native.Vibration      != 0,
                HasPressureAlert  = native.PressureAlert  != 0,
                HasLowLubricant   = native.LowLubricant   != 0,
                FaultCode         = native.FaultCode,
                AlertCount        = native.AlertCount,
            };
        }
        catch (DllNotFoundException)
        {
            // Native lib not built yet — fall back gracefully
            return ParsedFlags.Default;
        }
    }

    public static int Encode(ParsedFlags flags)
    {
        try
        {
            var native = new NativeStatusFlags
            {
                PrimaryStatus = flags.PrimaryStatus switch
                {
                    "Online"      => 0,
                    "Offline"     => 1,
                    "Maintenance" => 2,
                    "Fault"       => 3,
                    _             => 0
                },
                Overheating   = flags.HasOverheating   ? 1 : 0,
                Vibration     = flags.HasVibration     ? 1 : 0,
                PressureAlert = flags.HasPressureAlert ? 1 : 0,
                LowLubricant  = flags.HasLowLubricant  ? 1 : 0,
                FaultCode     = flags.FaultCode,
            };
            return (int)EncodeStatusFlagsNative(ref native);
        }
        catch (DllNotFoundException)
        {
            return 0;
        }
    }
}

public class ParsedFlags
{
    public string PrimaryStatus     { get; set; } = "Online";
    public bool   HasOverheating    { get; set; }
    public bool   HasVibration      { get; set; }
    public bool   HasPressureAlert  { get; set; }
    public bool   HasLowLubricant   { get; set; }
    public int    FaultCode         { get; set; }
    public int    AlertCount        { get; set; }

    public static ParsedFlags Default => new() { PrimaryStatus = "Online" };

    public List<string> ActiveAlerts()
    {
        var alerts = new List<string>();
        if (HasOverheating)   alerts.Add("Overheating");
        if (HasVibration)     alerts.Add("Abnormal vibration");
        if (HasPressureAlert) alerts.Add("Pressure out of range");
        if (HasLowLubricant)  alerts.Add("Low lubricant");
        return alerts;
    }
}
