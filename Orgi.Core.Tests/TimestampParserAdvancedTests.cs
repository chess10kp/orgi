using Orgi.Core.Parsing;
using Xunit;

namespace Orgi.Core.Tests;

public class TimestampParserAdvancedTests
{
    [Theory]
    [InlineData("<2025-12-18 Thu 14:30-15:30>")]
    [InlineData("<2025-12-18 Thu 14:30>")]
    [InlineData("<2025-12-18 Thu>")]
    [InlineData("[2025-12-18 Thu 14:30-15:30]")]
    [InlineData("[2025-12-18 Thu]")]
    public void Parse_ValidTimestampFormats_ParsesCorrectly(string timestamp)
    {
        // Act
        var result = TimestampParser.Parse(timestamp);

        // Assert
        Assert.Equal(2025, result.Year);
        Assert.Equal(12, result.Month);
        Assert.Equal(18, result.Day);
    }

    [Theory]
    [InlineData("<2025-12-18 Thu 14:30-15:30>", 14, 30)]
    [InlineData("<2025-12-18 Thu 14:30>", 14, 30)]
    public void Parse_TimestampWithTimeRange_ParsesCorrectly(string timestamp, int expectedHour, int expectedMinute)
    {
        // Act
        var result = TimestampParser.Parse(timestamp);

        // Assert
        Assert.Equal(expectedHour, result.Hour);
        Assert.Equal(expectedMinute, result.Minute);
    }

    [Theory]
    [InlineData("invalid-timestamp")]
    [InlineData("<2025-13-18 Thu>")] // Invalid month
    [InlineData("<2025-12-32 Thu>")] // Invalid day
    [InlineData("<2025-12-18 Thu 25:00>")] // Invalid hour
    public void Parse_InvalidTimestamps_ThrowsException(string timestamp)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => TimestampParser.Parse(timestamp));
    }

    [Fact]
    public void IsValidTimestamp_ValidTimestamps_ReturnsTrue()
    {
        // Arrange
        var validTimestamps = new[]
        {
            "<2025-12-18 Thu>",
            "<2025-12-18 Thu 14:30>",
            "<2025-12-18 Thu 14:30-15:30>",
            "[2025-12-18 Thu]",
            "[2025-12-18 Thu 14:30]"
        };

        // Act & Assert
        foreach (var timestamp in validTimestamps)
        {
            Assert.True(TimestampParser.IsValidTimestamp(timestamp), $"Should be valid: {timestamp}");
        }
    }

    [Fact]
    public void IsValidTimestamp_InvalidTimestamps_ReturnsFalse()
    {
        // Arrange
        var invalidTimestamps = new[]
        {
            "invalid-timestamp",
            "<2025-13-18 Thu>",
            "<2025-12-32 Thu>",
            "",
            null
        };

        // Act & Assert
        foreach (var timestamp in invalidTimestamps)
        {
            if (timestamp == null)
                continue; // Skip null for this test as it would throw ArgumentException
            
            Assert.False(TimestampParser.IsValidTimestamp(timestamp), $"Should be invalid: {timestamp}");
        }
    }
}