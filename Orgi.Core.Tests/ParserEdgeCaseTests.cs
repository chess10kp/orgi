using Orgi.Core.Model;
using Orgi.Core.Parsing;
using Xunit;

namespace Orgi.Core.Tests;

public class ParserEdgeCaseTests
{
    [Fact]
    public void Parse_PropertyValueWithColon_ParsesCorrectly()
    {
        // Arrange
        var content = @"* TODO Issue with colon in property
  :PROPERTIES:
  :ID: colon-001
  :TITLE: Issue with colon in property
  :DESCRIPTION: URL: https://example.com
  :CREATED: <2025-12-18 Thu>
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            Assert.Contains("https://example.com", issues.First().Description);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_TagsWithSpaces_ParsesCorrectly()
    {
        // Arrange
        var content = @"* TODO Issue with spaced tags : spaced tag :another spaced:
  :PROPERTIES:
  :ID: spacedtags-001
  :TITLE: Issue with spaced tags
  :DESCRIPTION: Tags with spaces
  :CREATED: <2025-12-18 Thu>
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            var tags = issues.First().Tags;
            Assert.Contains("spaced tag", tags);
            Assert.Contains("another spaced", tags);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_NestedPropertiesDrawer_IgnoresContentInside()
    {
        // Arrange
        var content = @"* TODO Issue with nested drawer
  :PROPERTIES:
  :ID: nested-001
  :TITLE: Issue with nested drawer
  :DESCRIPTION: Nested drawer test
  :CREATED: <2025-12-18 Thu>
  :CUSTOM_DRAWER:
  This should be ignored
  :END:
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            Assert.Equal("nested-001", issues.First().Id);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_MultiplePropertiesDrawers_UsesFirstOne()
    {
        // Arrange
        var content = @"* TODO Issue with multiple drawers
  :PROPERTIES:
  :ID: mult-001
  :TITLE: First drawer
  :DESCRIPTION: First description
  :CREATED: <2025-12-18 Thu>
  :END:

  :PROPERTIES:
  :ID: mult-002
  :TITLE: Second drawer
  :DESCRIPTION: Second description
  :CREATED: <2025-12-19 Fri>
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            // Should use the first properties drawer
            Assert.Equal("First drawer", issues.First().Title);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData("<2025-12-18 Thu 14:30-15:30>")]
    [InlineData("<2025-12-18 Thu 14:30++1d>")]
    [InlineData("<2025-12-18 Thu 14:30--1d>")]
    public void Parse_ComplexTimestampFormats_HandlesGracefully(string timestamp)
    {
        // Arrange
        var content = $@"* TODO Issue with complex timestamp
  :PROPERTIES:
  :ID: complex-ts-001
  :TITLE: Complex timestamp
  :DESCRIPTION: Complex timestamp test
  :CREATED: {timestamp}
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act & Assert
            // The timestamp parser might not support complex formats, but should handle them gracefully
            // either by parsing or throwing a controlled exception
            var result = Record.Exception(() => parser.Parse().ToList());

            // Should not crash catastrophically
            Assert.True(result == null || result is ArgumentException || result is FormatException);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_PropertiesWithLeadingWhitespace_ParsesCorrectly()
    {
        // Arrange
        var content = @"* TODO Issue with indented properties
   :PROPERTIES:
    :ID: indent-001
    :TITLE: Issue with indented properties
   :DESCRIPTION: Properties with indentation
    :CREATED: <2025-12-18 Thu>
   :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            Assert.Equal("indent-001", issues.First().Id);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_MalformedPropertyValue_ThrowsException()
    {
        // Arrange
        var content = @"* TODO Issue with malformed property
  :PROPERTIES:
  :ID: malformed-prop-001
  :TITLE: Issue with malformed property
  :DESCRIPTION:
  :CREATED: <2025-12-18 Thu>
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act & Assert
            // Empty property value should throw an exception
            Assert.Throws<ArgumentException>(() => parser.Parse().ToList());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_ZeroWidthCharactersInHeadline_HandlesGracefully()
    {
        // Arrange
        var content = @"* TODO Issue with zero-width chars​
  :PROPERTIES:
  :ID: zerowidth-001
  :TITLE: Issue with zero-width chars​
  :DESCRIPTION: Contains zero-width characters
  :CREATED: <2025-12-18 Thu>
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            Assert.Equal("zerowidth-001", issues.First().Id);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_TabIndentation_ParsesCorrectly()
    {
        // Arrange
        var content = @"* TODO Issue with tabs
	:PROPERTIES:
	:ID: tabs-001
	:TITLE: Issue with tab indentation
	:DESCRIPTION: Properties use tabs instead of spaces
	:CREATED: <2025-12-18 Thu>
	:END:

	Body with tabs too.";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            Assert.Equal("tabs-001", issues.First().Id);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_CompactFileWithoutBlankLines_ParsesCorrectly()
    {
        // Arrange
        var content = @"* TODO First issue
  :PROPERTIES:
  :ID: compact-001
  :TITLE: First Issue
  :DESCRIPTION: First description
  :CREATED: <2025-12-18 Thu>
  :END:
* TODO Second issue
  :PROPERTIES:
  :ID: compact-002
  :TITLE: Second Issue
  :DESCRIPTION: Second description
  :CREATED: <2025-12-18 Thu>
  :END:
* TODO Third issue
  :PROPERTIES:
  :ID: compact-003
  :TITLE: Third Issue
  :DESCRIPTION: Third description
  :CREATED: <2025-12-18 Thu>
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Equal(3, issues.Count);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_FileWithByteOrderMark_ParsesCorrectly()
    {
        // Arrange
        var content = "\uFEFF* TODO Issue with BOM\n" +
                     "  :PROPERTIES:\n" +
                     "  :ID: bom-001\n" +
                     "  :TITLE: Issue with BOM\n" +
                     "  :DESCRIPTION: File starts with BOM\n" +
                     "  :CREATED: <2025-12-18 Thu>\n" +
                     "  :END:\n";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            Assert.Equal("bom-001", issues.First().Id);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_ExtremelyLargeFile_HandlesPerformance()
    {
        // Arrange
        var contentBuilder = new System.Text.StringBuilder();

        // Create a file with 100 issues
        for (int i = 1; i <= 100; i++)
        {
            contentBuilder.AppendLine($"* TODO Issue {i:D3}");
            contentBuilder.AppendLine("  :PROPERTIES:");
            contentBuilder.AppendLine($"  :ID: perf-{i:D3}");
            contentBuilder.AppendLine($"  :TITLE: Performance Test Issue {i:D3}");
            contentBuilder.AppendLine($"  :DESCRIPTION: This is issue number {i}");
            contentBuilder.AppendLine("  :CREATED: <2025-12-18 Thu>");
            contentBuilder.AppendLine("  :END:");
            contentBuilder.AppendLine();
            contentBuilder.AppendLine($"  Body content for issue {i}.");
            contentBuilder.AppendLine();
        }

        var tempFile = CreateTempFile(contentBuilder.ToString());
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var startTime = DateTime.UtcNow;
            var issues = parser.Parse().ToList();
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.Equal(100, issues.Count);

            // Performance assertion - should parse 100 issues in reasonable time
            var parseTime = endTime - startTime;
            Assert.True(parseTime.TotalSeconds < 5, $"Parsing took too long: {parseTime.TotalSeconds} seconds");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private string CreateTempFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        return tempFile;
    }
}