using Orgi.Core.Parsing;

namespace Orgi.Core.Model;

public enum IssueState
{
    Todo,
    InProgress,
    Done,
    Kill
}

public enum Priority
{
    None,
    A,
    B,
    C
}

public class Issue
{
    public string Id { get; init; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; init; }
    public IssueState State { get; set; }
    public Priority Priority { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = new List<string>();

    public Issue(string id, string title, string description, DateTime createdAt, IssueState state = IssueState.Todo, Priority priority = Priority.None, IEnumerable<string>? tags = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        CreatedAt = createdAt;
        State = state;
        Priority = priority;
        Tags = tags?.ToList() ?? new List<string>();
    }

    public override string ToString() => $"[{Id}] {Title} ({State}) [{Priority}] {string.Join(" ", Tags.Select(t => $":{t}:"))}";

    public static Issue FromOrgEntry(OrgEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(entry.Headline))
        {
            throw new ArgumentException("Issue headline is missing or empty.", nameof(entry));
        }

        var props = entry.Properties;

        if (!props.TryGetValue("ID", out var id) ||
            !props.TryGetValue("CREATED", out var createdAtRaw))
        {
            throw new ArgumentException("Required properties (id, title, description, or created_at) are missing from the Org entry.", nameof(entry));
        }

        DateTime createdAt;
        try
        {
            createdAt = TimestampParser.Parse(createdAtRaw);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"The 'created_at' value '{createdAtRaw}' is not a valid org-mode timestamp: {ex.Message}", nameof(entry), ex);
        }

        // Parse priority from properties
        var priority = Priority.None;
        if (props.TryGetValue("priority", out var priorityStr) && Enum.TryParse<Priority>(priorityStr, true, out var parsedPriority))
        {
            priority = parsedPriority;
        }

        // Parse tags from properties
        var tags = new List<string>();
        if (props.TryGetValue("tags", out var tagsStr))
        {
            tags = tagsStr.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        return new Issue(id, entry.Headline, entry.Body, createdAt, entry.State, priority, tags);
    }
}