using Orgi.Core.Model;
using Orgi.Core.Parsing;
using Xunit;

namespace Orgi.Core.Tests;

public class ParserAdvancedTests
{
    private readonly string _testDataDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

    [Fact]
    public void Parse_MultipleHeadlineLevels_ParsesCorrectly()
    {
        // Arrange
        var content = @"* TODO Top level issue
  :PROPERTIES:
  :ID: top-001
  :TITLE: Top Level Issue
  :DESCRIPTION: A top level issue
  :CREATED: <2025-12-18 Thu>
  :END:

** DONE Sub level issue
   :PROPERTIES:
   :ID: sub-001
   :TITLE: Sub Level Issue
   :DESCRIPTION: A sub level issue
   :CREATED: <2025-12-18 Thu>
   :END:

*** INPROGRESS Deep level issue
    :PROPERTIES:
    :ID: deep-001
    :TITLE: Deep Level Issue
    :DESCRIPTION: A deeply nested issue
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
            Assert.Equal("top-001", issues[0].Id);
            Assert.Equal("sub-001", issues[1].Id);
            Assert.Equal("deep-001", issues[2].Id);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_HeadlineWithSpecialCharacters_ParsesCorrectly()
    {
        // Arrange
        var content = @"* TODO [#A] Issue with special chars: @#$%^&*()_+-={}[]|\/:;""'<>?,. :tag:special:
  :PROPERTIES:
  :ID: special-001
  :TITLE: Issue with special chars: @#$%^&*()_+-={}[]|\/:;""'<>?,.
  :DESCRIPTION: Description with special chars: !@#$%^&*()
  :CREATED: <2025-12-18 Thu 14:30>
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            var issue = issues.First();
            Assert.Equal("special-001", issue.Id);
            Assert.Contains("@#$%^&*()", issue.Title);
            Assert.Contains("!@#$%^&*()", issue.Description);
            Assert.Contains("special", issue.Tags);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_IssueWithMultilineBody_ParsesCorrectly()
    {
        // Arrange
        var content = @"* TODO Multi-line body issue
  :PROPERTIES:
  :ID: multiline-001
  :TITLE: Multi-line Body Issue
  :CREATED: <2025-12-18 Thu>
  :END:

  This is the first line of the body.
  This is the second line with some details.
  - Bullet point 1
  - Bullet point 2
  - Bullet point 3

  Final paragraph with more information.

* DONE Next issue
  :PROPERTIES:
  :ID: next-001
  :TITLE: Next Issue
  :DESCRIPTION: Simple issue
  :CREATED: <2025-12-18 Thu>
  :END:

  Simple body.";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Equal(2, issues.Count);
            var multilineIssue = issues.First();
            Assert.Contains("This is the first line of the body.", multilineIssue.Description);
            Assert.Contains("This is the second line", multilineIssue.Description);
            Assert.Contains("Bullet point 1", multilineIssue.Description);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_IssueWithoutPriority_HandlesGracefully()
    {
        // Arrange
        var content = @"* TODO Issue without priority
  :PROPERTIES:
  :ID: nopriority-001
  :TITLE: Issue Without Priority
  :DESCRIPTION: This issue has no priority cookie
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
            Assert.Equal(Priority.None, issues.First().Priority);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_IssueWithoutTags_HandlesGracefully()
    {
        // Arrange
        var content = @"* DONE [#B] Issue without tags
  :PROPERTIES:
  :ID: notags-001
  :TITLE: Issue Without Tags
  :DESCRIPTION: This issue has no tags
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
            Assert.Empty(issues.First().Tags);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData("CREATED", "<2025-12-18 Thu>")]
    [InlineData("created", "<2025-12-18 Thu>")]
    [InlineData("CREATED_AT", "<2025-12-18 Thu 14:30>")]
    [InlineData("created_at", "<2025-12-18 Thu 14:30>")]
    public void Parse_DifferentCreatedPropertyNames_ParsesCorrectly(string createdKey, string createdValue)
    {
        // Arrange
        var content = $@"* TODO Test issue
  :PROPERTIES:
  :ID: test-001
  :TITLE: Test Issue
  :DESCRIPTION: Test description
  :{createdKey}: {createdValue}
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            Assert.Equal(2025, issues.First().CreatedAt.Year);
            Assert.Equal(18, issues.First().CreatedAt.Day);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_IssueWithAdditionalProperties_ParsesCorrectly()
    {
        // Arrange
        var content = @"* TODO Issue with extra properties
  :PROPERTIES:
  :ID: extra-001
  :TITLE: Issue With Extra Properties
  :DESCRIPTION: Issue with additional properties
  :CREATED: <2025-12-18 Thu>
  :ASSIGNEE: john.doe@example.com
  :ESTIMATED_HOURS: 8
  :PROJECT: ORGI
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            var issue = issues.First();
            // Note: Additional properties are not exposed in the Issue model
            // but they should be parsed without errors
            Assert.Equal("extra-001", issue.Id);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_PropertiesDrawerWithWhitespace_ParsesCorrectly()
    {
        // Arrange
        var content = @"* TODO Issue with whitespace in properties
  :PROPERTIES:
  :ID: whitespace-001
  :TITLE:   Issue With Whitespace
  :DESCRIPTION:  Description with whitespace
  :CREATED:    <2025-12-18 Thu>
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            var issue = issues.First();
            // The parser should trim whitespace from property values
            Assert.Equal("Issue With Whitespace", issue.Title);
            Assert.Equal("Description with whitespace", issue.Description);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_UnterminatedPropertiesDrawer_ThrowsException()
    {
        // Arrange
        var content = @"* TODO Issue without END property
  :PROPERTIES:
  :ID: unterminated-001
  :TITLE: Unterminated Properties Drawer
  :DESCRIPTION: This properties drawer has no :END:
  :CREATED: <2025-12-18 Thu>

* TODO Next issue
  :PROPERTIES:
  :ID: next-001
  :TITLE: Next Issue
  :DESCRIPTION: This should fail parsing
  :CREATED: <2025-12-18 Thu>
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act & Assert
            Assert.Throws<FormatException>(() => parser.Parse().ToList());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_EmptyHeadline_ThrowsException()
    {
        // Arrange
        var content = @"*
  :PROPERTIES:
  :ID: empty-001
  :TITLE: Empty Headline
  :DESCRIPTION: This headline has no text
  :CREATED: <2025-12-18 Thu>
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act & Assert
            Assert.Throws<FormatException>(() => parser.Parse().ToList());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_FileWithOnlyEmptyLines_ReturnsEmptyList()
    {
        // Arrange
        var content = @"



   ";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Empty(issues);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_FileWithCommentsOnly_ReturnsEmptyList()
    {
        // Arrange
        var content = @"# This is a comment
# Another comment
   # Indented comment

# Final comment";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Empty(issues);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData("\r\n")] // Windows line endings
    [InlineData("\n")]   // Unix line endings
    [InlineData("\r")]   // Old Mac line endings
    public void Parse_DifferentLineEndings_ParsesCorrectly(string lineEnding)
    {
        // Arrange
        var content = $"* TODO Test issue{lineEnding}" +
                     $"  :PROPERTIES:{lineEnding}" +
                     $"  :ID: lineending-001{lineEnding}" +
                     $"  :TITLE: Test Issue{lineEnding}" +
                     $"  :DESCRIPTION: Test description{lineEnding}" +
                     $"  :CREATED: <2025-12-18 Thu>{lineEnding}" +
                     $"  :END:{lineEnding}" +
                     $"{lineEnding}" +
                     $"  Body content here.{lineEnding}";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            Assert.Equal("lineending-001", issues.First().Id);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_VeryLongHeadline_ParsesCorrectly()
    {
        // Arrange
        var longTitle = new string('A', 1000);
        var content = $@"* TODO {longTitle}
  :PROPERTIES:
  :ID: long-001
  :TITLE: {longTitle}
  :DESCRIPTION: Very long title test
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
            Assert.Equal(1000, issues.First().Title.Length);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_FileWithUtf8Characters_ParsesCorrectly()
    {
        // Arrange
        var content = @"* TODO Issue with UTF-8 chars: 🚀 αβγ 汉字 العربية
  :PROPERTIES:
  :ID: utf8-001
  :TITLE: Issue with UTF-8: 🚀 αβγ 汉字 العربية
  :DESCRIPTION: Description with emojis and unicode: 🎉 ✓ ✓ ✗ ☕ 🌟
  :CREATED: <2025-12-18 Thu>
  :TAGS: utf8:emoji:unicode
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            var issue = issues.First();
            Assert.Contains("🚀", issue.Title);
            Assert.Contains("αβγ", issue.Title);
            Assert.Contains("汉字", issue.Title);
            Assert.Contains("العربية", issue.Title);
            Assert.Contains("🎉", issue.Description);
            Assert.Contains("utf8", issue.Tags);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_MalformedHeadlineWithoutState_ThrowsException()
    {
        // Arrange
        var content = @"* Not a valid headline without state
  :PROPERTIES:
  :ID: malformed-001
  :TITLE: Malformed Headline
  :DESCRIPTION: Missing state keyword
  :CREATED: <2025-12-18 Thu>
  :END:";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act & Assert
            Assert.Throws<FormatException>(() => parser.Parse().ToList());
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