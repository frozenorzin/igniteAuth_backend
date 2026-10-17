namespace IgAPI.Security;

/// <summary>Explicitly reviewed read-only operations for the reference system.</summary>
public static class ReadOnlyCommands
{
    private static readonly HashSet<(string Subsystem, string Command)> Allowed =
    [
        ("GPIO", "ReadPinState"),
        ("ADC", "ReadADCChannel"),
        ("Memory", "ReadMemoryRegion"),
        ("FirmwareManager", "GetFirmwareVersion"),
        ("SystemMonitor", "ReadSystemHealth")
    ];

    public static bool Contains(string subsystem, string command) => Allowed.Contains((subsystem, command));
}
