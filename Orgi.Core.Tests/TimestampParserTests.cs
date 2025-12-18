using Orgi.Core.Model;
using Orgi.Core.Parsing;
using Xunit;

namespace Orgi.Core.Tests;

public class TimestampParserTests
{
    [Theory]
    [InlineData("<2025-09-12 Fri>", 2025, 9, 12, 0, 0)]
    [InlineData("<2025-09-12 Fri 14:30>", 2025, 9, 12, 14, 30)]
    [InlineData("[2025-09-12 Fri]", 2025, 9, 12, 0, 0)]
    [InlineData("[2025-09-12 Fri 09:15]", 2025, 9, 12, 9, 15)]
    public void Parse_ValidTimestamps_ReturnsCorrectDateTime(string timestamp, int year, int month, int day, int hour, int minute)
    {
        // Act
        var result = TimestampParser.Parse(timestamp);

        // Assert
        Assert.Equal(year, result.Year);
        Assert.Equal(month, result.Month);
        Assert.Equal(day, result.Day);
        Assert.Equal(hour, result.Hour);
        Assert.Equal(minute, result.Minute);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("2025-09-12")]
    [InlineData("<2025-13-45>")] // Invalid date
    [InlineData("<2025-09-12 25:70>")] // Invalid time
    public void Parse_InvalidTimestamps_ThrowsException(string timestamp)
    {
        // Act & Assert
        Assert.ThrowsAny<Exception>(() => TimestampParser.Parse(timestamp));
    }

    [Fact]
    public void Parse_EmptyTimestamp_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimestampParser.Parse(""));
    }

    [Theory]
    [InlineData("<2025-09-12 Fri>", true)]
    [InlineData("[2025-09-12 Fri]", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void IsValidTimestamp_ReturnsCorrectResult(string timestamp, bool expected)
    {
        // Act
        var result = TimestampParser.IsValidTimestamp(timestamp);

        // Assert
        Assert.Equal(expected, result);
    }
}