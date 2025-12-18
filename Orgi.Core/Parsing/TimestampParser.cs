using System.Text.RegularExpressions;

namespace Orgi.Core.Parsing;

public static class TimestampParser
{
    private static readonly Regex OrgTimestampRegex = new Regex(
        @"^<(\d{4})-(\d{2})-(\d{2})(?:\s+[A-Za-z]+)?(?:\s+(\d{2}):(\d{2})(?:-(\d{2}):(\d{2}))?)?>$",
        RegexOptions.Compiled
    );

    private static readonly Regex InactiveOrgTimestampRegex = new Regex(
        @"^\[(\d{4})-(\d{2})-(\d{2})(?:\s+[A-Za-z]+)?(?:\s+(\d{2}):(\d{2})(?:-(\d{2}):(\d{2}))?)?\]$",
        RegexOptions.Compiled
    );

    public static DateTime Parse(string timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
        {
            throw new ArgumentException("Timestamp cannot be null or empty", nameof(timestamp));
        }

        // Try active timestamp format <YYYY-MM-DD ...>
        var match = OrgTimestampRegex.Match(timestamp.Trim());
        if (match.Success)
        {
            return ParseMatch(match);
        }

        // Try inactive timestamp format [YYYY-MM-DD ...]
        match = InactiveOrgTimestampRegex.Match(timestamp.Trim());
        if (match.Success)
        {
            return ParseMatch(match);
        }

        throw new FormatException($"Invalid org-mode timestamp format: {timestamp}");
    }

    private static DateTime ParseMatch(Match match)
    {
        var year = int.Parse(match.Groups[1].Value);
        var month = int.Parse(match.Groups[2].Value);
        var day = int.Parse(match.Groups[3].Value);

        var hour = 0;
        var minute = 0;

        if (match.Groups[4].Success) // Time is present
        {
            hour = int.Parse(match.Groups[4].Value);
            minute = int.Parse(match.Groups[5].Value);
        }

        try
        {
            return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new FormatException($"Invalid date/time values in timestamp: {match.Value}", ex);
        }
    }

    public static bool IsValidTimestamp(string timestamp)
    {
        try
        {
            Parse(timestamp);
            return true;
        }
        catch
        {
            return false;
        }
    }
}