using Orgi.Core.Model;
using Xunit;

namespace Orgi.Core.Tests;

public class IssueTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesIssue()
    {
        // Arrange
        var id = "test-001";
        var title = "Test Issue";
        var description = "Test description";
        var createdAt = new DateTime(2025, 9, 12, 14, 30, 0);
        var state = IssueState.Todo;
        var priority = Priority.A;
        var tags = new[] { "urgent", "bug" };

        // Act
        var issue = new Issue(id, title, description, createdAt, state, priority, tags);

        // Assert
        Assert.Equal(id, issue.Id);
        Assert.Equal(title, issue.Title);
        Assert.Equal(description, issue.Description);
        Assert.Equal(createdAt, issue.CreatedAt);
        Assert.Equal(state, issue.State);
        Assert.Equal(priority, issue.Priority);
        Assert.Equal(tags, issue.Tags);
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new Issue(null!, "Title", "Description", DateTime.Now));
    }

    [Fact]
    public void FromOrgEntry_ValidEntry_CreatesIssue()
    {
        // Arrange
        var properties = new Dictionary<string, string>
        {
            ["ID"] = "test-001",
            ["TITLE"] = "Test Issue",
            ["DESCRIPTION"] = "Test description",
            ["created_at"] = "<2025-09-12 Fri 14:30>",
            ["priority"] = "A",
            ["tags"] = "urgent:bug"
        };
        var entry = new OrgEntry("Test Issue", IssueState.Todo, properties, "Body text");

        // Act
        var issue = Issue.FromOrgEntry(entry);

        // Assert
        Assert.Equal("test-001", issue.Id);
        Assert.Equal("Test Issue", issue.Title);
        Assert.Equal("Test description", issue.Description);
        Assert.Equal(IssueState.Todo, issue.State);
        Assert.Equal(Priority.A, issue.Priority);
        Assert.Equal(new[] { "urgent", "bug" }, issue.Tags);
    }

    [Fact]
    public void FromOrgEntry_MissingRequiredProperties_ThrowsArgumentException()
    {
        // Arrange
        var properties = new Dictionary<string, string>
        {
            ["ID"] = "test-001",
            ["TITLE"] = "Test Issue"
            // Missing description and created_at
        };
        var entry = new OrgEntry("Test Issue", IssueState.Todo, properties, "Body text");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Issue.FromOrgEntry(entry));
    }

    [Fact]
    public void FromOrgEntry_InvalidTimestamp_ThrowsArgumentException()
    {
        // Arrange
        var properties = new Dictionary<string, string>
        {
            ["ID"] = "test-001",
            ["TITLE"] = "Test Issue",
            ["DESCRIPTION"] = "Test description",
            ["created_at"] = "invalid-timestamp"
        };
        var entry = new OrgEntry("Test Issue", IssueState.Todo, properties, "Body text");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Issue.FromOrgEntry(entry));
    }

    [Fact]
    public void FromOrgEntry_NullEntry_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Issue.FromOrgEntry(null!));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var issue = new Issue("test-001", "Test Issue", "Description", DateTime.Now, 
            IssueState.Todo, Priority.A, new[] { "urgent", "bug" });

        // Act
        var result = issue.ToString();

        // Assert
        Assert.Contains("[test-001]", result);
        Assert.Contains("Test Issue", result);
        Assert.Contains("(Todo)", result);
        Assert.Contains("[A]", result);
        Assert.Contains(":urgent:", result);
        Assert.Contains(":bug:", result);
    }
}