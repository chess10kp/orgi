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
    public IReadOnlyDictionary<string, string> Properties { get; init; }

    /// <summary>
    /// Gets whether this issue has a source file reference
    /// </summary>
    public bool HasSourceReference => Properties.ContainsKey("SOURCE_FILE");

    /// <summary>
    /// Gets the source file path if this issue was gathered from source code
    /// </summary>
    public string? SourceFile => HasSourceReference ? Properties["SOURCE_FILE"] : null;

    /// <summary>
    /// Gets the source line number if this issue was gathered from source code
    /// </summary>
    public int? SourceLine => HasSourceReference && Properties.TryGetValue("SOURCE_LINE", out var lineStr) && int.TryParse(lineStr, out var line) ? line : null;

    /// <summary>
    /// Gets the source column number if this issue was gathered from source code
    /// </summary>
    public int? SourceColumn => HasSourceReference && Properties.TryGetValue("SOURCE_COLUMN", out var colStr) && int.TryParse(colStr, out var col) ? col : null;

    /// <summary>
    /// Gets the source UUID for tracking changes if this issue was gathered from source code
    /// </summary>
    public string? SourceUuid => HasSourceReference && Properties.TryGetValue("SOURCE_UUID", out var uuid) ? uuid : null;

    public Issue(string id, string title, string description, DateTime createdAt, IssueState state = IssueState.Todo, Priority priority = Priority.None, IEnumerable<string>? tags = null, IReadOnlyDictionary<string, string>? properties = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        CreatedAt = createdAt;
        State = state;
        Priority = priority;
        Tags = tags?.ToList() ?? new List<string>();
        Properties = properties != null ? new Dictionary<string, string>(properties) : new Dictionary<string, string>();
    }

    public override string ToString()
    {
        var sourceInfo = HasSourceReference ? $" ({SourceFile}:{SourceLine})" : "";
        return $"[{Id}] {Title} ({State}) [{Priority}] {string.Join(" ", Tags.Select(t => $":{t}:"))}{sourceInfo}";
    }

    /// <summary>
    /// Generates a compact UUID for identifying gathered issues
    /// </summary>
    public static string GenerateUuid()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    public static Issue FromOrgEntry(OrgEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(entry.Headline))
        {
            throw new ArgumentException("Issue headline is missing or empty.", nameof(entry));
        }

        var props = entry.Properties;

        if (!props.TryGetValue("ID", out var id) || string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("Required properties (ID and created_at) are missing from the Org entry.", nameof(entry));
        }

        var createdAtRaw = string.Empty;
        var createdKeys = new[] { "CREATED", "CREATED_AT", "created_at", "created" };
        foreach (var key in createdKeys)
        {
            if (props.TryGetValue(key, out createdAtRaw))
            {
                break;
            }
        }
        if (string.IsNullOrEmpty(createdAtRaw))
        {
            throw new ArgumentException("Required properties (ID and created_at) are missing from the Org entry.", nameof(entry));
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

        // Parse title from properties or headline
        var title = entry.Headline;
        if (props.TryGetValue("TITLE", out var titleProp))
        {
            title = titleProp;
        }

        // Parse description from properties or body
        var description = entry.Body;
        if (props.TryGetValue("DESCRIPTION", out var descProp))
        {
            description = descProp;
        }

        // Parse tags from properties
        var tags = new List<string>();
        if (props.TryGetValue("tags", out var tagsStr))
        {
            tags = tagsStr.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        return new Issue(id, title, description, createdAt, entry.State, priority, tags, entry.Properties);
    }
}