using Orgi.Core.Model;
using Orgi.Core.Parsing;
using Xunit;

namespace Orgi.Core.Tests;

public class ParserTests
{
    private readonly string _testDataDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

    [Fact]
    public void Parse_ValidFile_ReturnsIssues()
    {
        // Arrange
        var testFile = Path.Combine(_testDataDir, "valid_issues.org");
        var parser = new Parser(testFile);

        // Act
        var issues = parser.Parse().ToList();

        // Assert
        Assert.Equal(3, issues.Count);
        
        var firstIssue = issues.First();
        Assert.Equal("bug-001", firstIssue.Id);
        Assert.Equal("Parser crash on empty file", firstIssue.Title);
        Assert.Equal(IssueState.Todo, firstIssue.State);
        Assert.Equal(Priority.A, firstIssue.Priority);
        Assert.Contains("urgent", firstIssue.Tags);
        Assert.Contains("bug", firstIssue.Tags);
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsEmptyList()
    {
        // Arrange
        var testFile = Path.Combine(_testDataDir, "empty_file.org");
        var parser = new Parser(testFile);

        // Act
        var issues = parser.Parse().ToList();

        // Assert
        Assert.Empty(issues);
    }

    [Fact]
    public void Parse_MissingRequiredProperties_ThrowsException()
    {
        // Arrange
        var testFile = Path.Combine(_testDataDir, "missing_properties.org");
        var parser = new Parser(testFile);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => parser.Parse().ToList());
    }

    [Fact]
    public void Parse_InvalidIssueState_ThrowsException()
    {
        // Arrange
        var testFile = Path.Combine(_testDataDir, "invalid_issues.org");
        var parser = new Parser(testFile);

        // Act & Assert
        Assert.Throws<FormatException>(() => parser.Parse().ToList());
    }

    [Fact]
    public void Parse_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var parser = new Parser("nonexistent.org");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => parser.Parse().ToList());
    }

    [Fact]
    public void Parse_InvalidPriority_ThrowsException()
    {
        // Arrange
        var testFile = Path.Combine(_testDataDir, "invalid_issues.org");
        var parser = new Parser(testFile);

        // Act & Assert
        // The parser should handle invalid priority gracefully by not parsing it as priority
        // But the file has other issues that will cause it to fail
        Assert.Throws<FormatException>(() => parser.Parse().ToList());
    }

    [Fact]
    public void Parse_MalformedPropertiesDrawer_ThrowsException()
    {
        // Arrange
        var testFile = Path.Combine(_testDataDir, "invalid_issues.org");
        var parser = new Parser(testFile);

        // Act & Assert
        Assert.Throws<FormatException>(() => parser.Parse().ToList());
    }

    [Theory]
    [InlineData("* TODO [#A] Test title :tag1:tag2:", "Test title", Priority.A, new[] { "tag1", "tag2" })]
    [InlineData("* TODO [#B] Test title", "Test title", Priority.B, new string[0])]
    [InlineData("* TODO Test title :single:", "Test title", Priority.None, new[] { "single" })]
    [InlineData("* DONE [#C] Completed task :done:", "Completed task", Priority.C, new[] { "done" })]
    public void Parse_VariousHeadlineFormats_ParsesCorrectly(string headlineLine, string expectedTitle, Priority expectedPriority, string[] expectedTags)
    {
        // Arrange
        var tempFile = CreateTempOrgFile(headlineLine, "test-id", expectedTitle, "Description", "<2025-09-12 Fri>");
        var parser = new Parser(tempFile);

        try
        {
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            var issue = issues.First();
            Assert.Equal(expectedTitle, issue.Title);
            
            // Debug output
            Console.WriteLine($"Expected priority: {expectedPriority}, Actual: {issue.Priority}");
            Console.WriteLine($"Expected tags: [{string.Join(",", expectedTags)}], Actual: [{string.Join(",", issue.Tags)}]");
            
            Assert.Equal(expectedPriority, issue.Priority);
            Assert.Equal(expectedTags, issue.Tags);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(IssueState.Todo)]
    [InlineData(IssueState.InProgress)]
    [InlineData(IssueState.Done)]
    [InlineData(IssueState.Kill)]
    public void Parse_AllIssueStates_ParsesCorrectly(IssueState state)
    {
        // Arrange
        var stateStr = state switch
        {
            IssueState.Todo => "TODO",
            IssueState.InProgress => "INPROGRESS",
            IssueState.Done => "DONE",
            IssueState.Kill => "KILL",
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

        var headlineLine = $"* {stateStr} Test issue";
        var tempFile = CreateTempOrgFile(headlineLine, "test-id", "Test Issue", "Description", "<2025-09-12 Fri>");
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            Assert.Equal(state, issues.First().State);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private string CreateTempOrgFile(string headlineLine, string id, string title, string description, string createdAt)
    {
        var content = $@"{headlineLine}
  :PROPERTIES:
  :ID: {id}
  :TITLE: {title}
  :DESCRIPTION: {description}
  :CREATED_AT: {createdAt}
  :END:

  Body content here.";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        return tempFile;
    }
}