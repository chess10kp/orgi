using System.Collections.ObjectModel;

namespace Orgi.Core.Model;

/// <summary>
/// Represents a reference to a TODO item found in source code
/// </summary>
public record SourceReference(
    string FilePath,
    int LineNumber,
    int ColumnNumber,
    string OriginalLine,
    string TodoKeyword,
    string TodoText,
    string CommentStyle
)
{
    /// <summary>
    /// Gets the relative file path from the current working directory
    /// </summary>
    public string RelativePath
    {
        get
        {
            var currentDir = Directory.GetCurrentDirectory();
            if (FilePath.StartsWith(currentDir))
            {
                return FilePath[currentDir.Length..].TrimStart('/', '\\');
            }
            return FilePath;
        }
    }

    /// <summary>
    /// Gets the full todo text including the keyword and the description
    /// </summary>
    public string FullTodoText => $"{TodoKeyword}: {TodoText}";

    /// <summary>
    /// Gets a hash of the todo content for change detection
    /// </summary>
    public string ContentHash => $"{TodoKeyword}:{TodoText}".GetHashCode().ToString("X");
}

/// <summary>
/// An issue that was gathered from source code
/// </summary>
public class GatheredIssue : Issue
{
    public SourceReference SourceReference { get; init; }
    public string OrgiUuid { get; init; }

    public GatheredIssue(
        string id,
        string title,
        string description,
        DateTime createdAt,
        SourceReference sourceReference,
        string orgiUuid,
        IssueState state = IssueState.Todo,
        Priority priority = Priority.None,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, string>? properties = null
    ) : base(id, title, description, createdAt, state, priority, tags, CreatePropertiesWithSource(sourceReference, properties))
    {
        SourceReference = sourceReference ?? throw new ArgumentNullException(nameof(sourceReference));
        OrgiUuid = orgiUuid ?? throw new ArgumentNullException(nameof(orgiUuid));
    }

    /// <summary>
    /// Creates a properties dictionary that includes source reference information
    /// </summary>
    private static IReadOnlyDictionary<string, string> CreatePropertiesWithSource(
        SourceReference sourceRef,
        IReadOnlyDictionary<string, string>? existingProperties)
    {
        var properties = new Dictionary<string, string>(existingProperties ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase)
        {
            ["SOURCE_FILE"] = sourceRef.RelativePath,
            ["SOURCE_LINE"] = sourceRef.LineNumber.ToString(),
            ["SOURCE_COLUMN"] = sourceRef.ColumnNumber.ToString(),
            ["SOURCE_UUID"] = sourceRef.ContentHash
        };

        return new ReadOnlyDictionary<string, string>(properties);
    }
}