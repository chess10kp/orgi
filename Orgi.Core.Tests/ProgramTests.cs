using Orgi.Core;
using Xunit;

namespace Orgi.Core.Tests;

public class ProgramTests
{
    [Fact]
    public void ListIssues_ValidFile_ReturnsFormattedOutput()
    {
        // Arrange
        var testFile = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "valid_issues.org");

        // Act
        var output = Program.ListIssues(testFile);

        // Assert
        Assert.Contains("Found 3 issues:", output);
        Assert.Contains("bug-001: Parser crash on empty file", output);
        Assert.Contains("(Todo)", output);
        Assert.Contains("[A]", output);
    }

    [Fact]
    public void ListIssues_EmptyFile_ReturnsNoIssues()
    {
        // Arrange
        var testFile = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "empty_file.org");

        // Act
        var output = Program.ListIssues(testFile);

        // Assert
        Assert.Equal("Found 0 issues:", output);
    }

    [Fact]
    public void ListIssues_NonExistentFile_ReturnsErrorMessage()
    {
        // Arrange
        var testFile = "nonexistent.org";

        // Act
        var result = Program.ListIssues(testFile);

        // Assert
        Assert.Contains("Error: File not found", result);
        Assert.Contains(testFile, result);
    }

    [Fact]
    public void AddIssueToFile_AppendsCorrectContent()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "# Test org file\n");

        var title = "Test Task";
        var priority = "A";
        var state = "TODO";
        var tags = " :urgent:bug:";
        var body = "This is a test body";

        // Act
        Program.AddIssueToFile(tempFile, title, priority, state, tags, body);

        // Assert
        var content = File.ReadAllText(tempFile);
        Assert.Contains($"* {state} [#{priority}] {title}{tags}", content);
        Assert.Contains(":ID: task-", content);
        Assert.Contains(":TITLE: Test Task", content);
        Assert.Contains(":CREATED: <", content);
        Assert.Contains("This is a test body", content);

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public void AddIssueToFile_NoPriority_AppendsWithoutPriority()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "# Test org file\n");

        var title = "Test Task";
        var priority = "";
        var state = "TODO";
        var tags = "";
        var body = "Body";

        // Act
        Program.AddIssueToFile(tempFile, title, priority, state, tags, body);

        // Assert
        var content = File.ReadAllText(tempFile);
        Assert.Contains($"* {state} {title}", content);
        Assert.DoesNotContain("[#", content);

        // Cleanup
        File.Delete(tempFile);
    }
}