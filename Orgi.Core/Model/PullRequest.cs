using System.Text.Json;

namespace Orgi.Core.Model;

public class PullRequest
{
    public string Id { get; init; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Author { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; set; }
    public PullRequestState State { get; set; }
    public string SourceBranch { get; init; }
    public string TargetBranch { get; init; }
    public List<PullRequestReview> Reviews { get; set; } = new();
    public List<string> Commits { get; set; } = new();
    public string? MergedBy { get; set; }
    public DateTime? MergedAt { get; set; }
    public string? MergeCommit { get; set; }

    public PullRequest(
        string id,
        string title,
        string description,
        string author,
        string sourceBranch,
        string targetBranch)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Author = author ?? throw new ArgumentNullException(nameof(author));
        SourceBranch = sourceBranch ?? throw new ArgumentNullException(nameof(sourceBranch));
        TargetBranch = targetBranch ?? throw new ArgumentNullException(nameof(targetBranch));
        CreatedAt = DateTime.Now;
        State = PullRequestState.Open;
    }

    public bool IsApproved => Reviews.Any(r => r.Approved);
    public bool IsDenied => Reviews.Any(r => !r.Approved);
    public bool CanMerge => IsApproved && !IsDenied;

    public static string GenerateId() => $"pr-{DateTime.Now.ToString("yyyyMMddHHmmss")}";

    public static PullRequest FromJson(string json)
    {
        return JsonSerializer.Deserialize<PullRequest>(json) ?? throw new JsonException("Failed to deserialize PR");
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}

public class PullRequestReview
{
    public string Reviewer { get; init; }
    public bool Approved { get; init; }
    public string? Comment { get; init; }
    public DateTime ReviewedAt { get; init; }

    public PullRequestReview(string reviewer, bool approved, string? comment = null)
    {
        Reviewer = reviewer ?? throw new ArgumentNullException(nameof(reviewer));
        Approved = approved;
        Comment = comment;
        ReviewedAt = DateTime.Now;
    }
}
