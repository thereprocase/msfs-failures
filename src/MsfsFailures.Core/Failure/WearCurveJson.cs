using System.Text.Json;

namespace MsfsFailures.Core.Failure;

/// <summary>
/// Lightweight reader for the WearCurve JSON stored on ComponentTemplate.
/// Only extracts FailureEngine-relevant fields; other fields are left to WearEngine.
///
/// Expected shape (all fields optional):
/// {
///   "weibullBeta":        1.58,
///   "weibullAlphaHours":  2000.0
/// }
/// </summary>
internal static class WearCurveJson
{
    public static (double? Beta, double? Alpha) ReadWeibull(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (null, null);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            double? beta  = TryGetDouble(root, "weibullBeta");
            double? alpha = TryGetDouble(root, "weibullAlphaHours");
            return (beta, alpha);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static double? TryGetDouble(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.Number
            && prop.TryGetDouble(out double v))
            return v;

        return null;
    }
}
