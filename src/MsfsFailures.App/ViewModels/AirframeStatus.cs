namespace MsfsFailures.App.ViewModels;

public enum AirframeStatus
{
    Airworthy,
    Squawks,
    MxDue,
    Grounded,
}

public static class AirframeStatusExtensions
{
    public static string ToLabel(this AirframeStatus s) => s switch
    {
        AirframeStatus.Airworthy => "AIRWORTHY",
        AirframeStatus.Squawks   => "SQUAWKS",
        AirframeStatus.MxDue     => "MX DUE",
        AirframeStatus.Grounded  => "GROUNDED",
        _ => s.ToString().ToUpperInvariant(),
    };
}
