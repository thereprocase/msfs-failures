using System.Text.Json;
using System.Text.RegularExpressions;

namespace MsfsFailures.App.Services;

/// <summary>
/// Matcher for aircraft titles against sim match rules stored in ModelRef.SimMatchRulesJson.
/// JSON shape: { "contains": ["Caravan", "208B"], "regex": ["^Cessna 208"] }
/// Both "contains" and "regex" fields are optional.
/// "contains" values are matched case-insensitively as substrings.
/// "regex" values are matched case-insensitively as regex patterns.
/// Returns true if the aircraft title matches any rule in either list.
/// Empty, null, or malformed JSON returns false.
/// </summary>
public static class SimMatchRules
{
    /// <summary>
    /// Test if the aircraft title matches the rules in SimMatchRulesJson.
    /// </summary>
    /// <param name="simMatchRulesJson">JSON containing "contains" and/or "regex" arrays.</param>
    /// <param name="aircraftTitle">The aircraft title to test (e.g., "Asobo Cessna 172 G1000").</param>
    /// <param name="atcModel">The ATC model code (used for future enhancement; currently unused).</param>
    /// <returns>True if any rule matches, false otherwise.</returns>
    public static bool Matches(string? simMatchRulesJson, string aircraftTitle, string atcModel)
    {
        if (string.IsNullOrWhiteSpace(simMatchRulesJson) || string.IsNullOrWhiteSpace(aircraftTitle))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(simMatchRulesJson);
            var root = doc.RootElement;

            // Test "contains" rules (case-insensitive substring match)
            if (root.TryGetProperty("contains", out var containsElem) && containsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in containsElem.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var pattern = item.GetString();
                        if (!string.IsNullOrWhiteSpace(pattern) &&
                            aircraftTitle.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            // Test "regex" rules (case-insensitive regex match)
            if (root.TryGetProperty("regex", out var regexElem) && regexElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in regexElem.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var pattern = item.GetString();
                        if (!string.IsNullOrWhiteSpace(pattern))
                        {
                            try
                            {
                                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                                if (regex.IsMatch(aircraftTitle))
                                    return true;
                            }
                            catch (ArgumentException)
                            {
                                // Invalid regex pattern — skip it
                            }
                        }
                    }
                }
            }

            return false;
        }
        catch (JsonException)
        {
            // Malformed JSON — return false
            return false;
        }
    }
}
