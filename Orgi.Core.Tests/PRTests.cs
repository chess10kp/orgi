using Orgi.Core.Model;
using Xunit;

namespace Orgi.Core.Tests;

public class PRTests
{
    [Fact]
    public void PullRequest_GenerateId_ReturnsValidId()
    {
        // Act
        var id = PullRequest.GenerateId();

        // Assert
        Assert.StartsWith("pr-", id);
        Assert.True(id.Length > 3);
    }

    [Fact]
    public void PullRequest_Creation_SetsCorrectProperties()
    {
        // Arrange
        var title = "Test PR";
        var description = "Test description";
        var author = "testuser";
        var sourceBranch = "feature-branch";
        var targetBranch = "main";

        // Act
        var pr = new PullRequest("pr-123", title, description, author, sourceBranch, targetBranch);

        // Assert
        Assert.Equal("pr-123", pr.Id);
        Assert.Equal(title, pr.Title);
        Assert.Equal(description, pr.Description);
        Assert.Equal(author, pr.Author);
        Assert.Equal(sourceBranch, pr.SourceBranch);
        Assert.Equal(targetBranch, pr.TargetBranch);
        Assert.Equal(PullRequestState.Open, pr.State);
        Assert.NotEqual(default, pr.CreatedAt);
    }

    [Fact]
    public void PullRequest_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var pr = new PullRequest("pr-123", "Test PR", "Test description", "testuser", "feature-branch", "main");
        pr.Reviews.Add(new PullRequestReview("reviewer1", true, "Looks good"));

        // Act
        var json = pr.ToJson();
        var deserialized = PullRequest.FromJson(json);

        // Assert
        Assert.Equal(pr.Id, deserialized.Id);
        Assert.Equal(pr.Title, deserialized.Title);
        Assert.Equal(pr.Description, deserialized.Description);
        Assert.Equal(pr.Author, deserialized.Author);
        Assert.Equal(pr.SourceBranch, deserialized.SourceBranch);
        Assert.Equal(pr.TargetBranch, deserialized.TargetBranch);
        Assert.Equal(pr.State, deserialized.State);
        Assert.Single(deserialized.Reviews);
        Assert.Equal("reviewer1", deserialized.Reviews[0].Reviewer);
        Assert.True(deserialized.Reviews[0].Approved);
        Assert.Equal("Looks good", deserialized.Reviews[0].Comment);
    }

    [Fact]
    public void PullRequest_IsApproved_ReturnsTrue_WhenApproved()
    {
        // Arrange
        var pr = new PullRequest("pr-123", "Test PR", "Test description", "testuser", "feature-branch", "main");
        pr.Reviews.Add(new PullRequestReview("reviewer1", true));

        // Act & Assert
        Assert.True(pr.IsApproved);
    }

    [Fact]
    public void PullRequest_IsApproved_ReturnsFalse_WhenNotApproved()
    {
        // Arrange
        var pr = new PullRequest("pr-123", "Test PR", "Test description", "testuser", "feature-branch", "main");
        pr.Reviews.Add(new PullRequestReview("reviewer1", false));

        // Act & Assert
        Assert.False(pr.IsApproved);
    }

    [Fact]
    public void PullRequest_IsDenied_ReturnsTrue_WhenDenied()
    {
        // Arrange
        var pr = new PullRequest("pr-123", "Test PR", "Test description", "testuser", "feature-branch", "main");
        pr.Reviews.Add(new PullRequestReview("reviewer1", false));

        // Act & Assert
        Assert.True(pr.IsDenied);
    }

    [Fact]
    public void PullRequest_CanMerge_ReturnsTrue_WhenApprovedAndNotDenied()
    {
        // Arrange
        var pr = new PullRequest("pr-123", "Test PR", "Test description", "testuser", "feature-branch", "main");
        pr.Reviews.Add(new PullRequestReview("reviewer1", true));

        // Act & Assert
        Assert.True(pr.CanMerge);
    }

    [Fact]
    public void PullRequest_CanMerge_ReturnsFalse_WhenDenied()
    {
        // Arrange
        var pr = new PullRequest("pr-123", "Test PR", "Test description", "testuser", "feature-branch", "main");
        pr.Reviews.Add(new PullRequestReview("reviewer1", false));

        // Act & Assert
        Assert.False(pr.CanMerge);
    }

    [Fact]
    public void PullRequest_CanMerge_ReturnsFalse_WhenNotApproved()
    {
        // Arrange
        var pr = new PullRequest("pr-123", "Test PR", "Test description", "testuser", "feature-branch", "main");

        // Act & Assert
        Assert.False(pr.CanMerge);
    }
}
