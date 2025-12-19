using Orgi.Core.Model;
using Orgi.Core.Parsing;
using Orgi.Core.Rewriting;
using Orgi.Core.Discovery;

namespace Orgi.Core.Sync;

/// <summary>
/// Synchronizes source code TODOs with orgi issues
/// </summary>
public class IssueSynchronizer
{
    private readonly SourceTodoParser _todoParser;
    private readonly SourceFileDiscovery _fileDiscovery;
    private readonly SourceFileRewriter _fileRewriter;

    public IssueSynchronizer()
    {
        _todoParser = new SourceTodoParser();
        _fileDiscovery = new SourceFileDiscovery();
        _fileRewriter = new SourceFileRewriter();
    }

    public IssueSynchronizer(SourceFileRewriter fileRewriter)
    {
        _todoParser = new SourceTodoParser();
        _fileDiscovery = new SourceFileDiscovery();
        _fileRewriter = fileRewriter ?? throw new ArgumentNullException(nameof(fileRewriter));
    }

    /// <summary>
    /// Gathers TODOs from source files and creates corresponding orgi issues
    /// </summary>
    public GatherResult GatherFromSource(string sourceDirectory, string orgFilePath, bool dryRun = false)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");
        }

        // Discover source files
        var sourceFiles = _fileDiscovery.DiscoverFiles(sourceDirectory).ToList();

        // Extract TODOs from source files
        var sourceTodos = _todoParser.ExtractTodos(sourceFiles).ToList();

        // Load existing orgi issues
        var existingIssues = LoadExistingIssues(orgFilePath).ToList();

        // Match source TODOs with existing issues
        var matchedIssues = new List<(SourceReference SourceTodo, Issue OrgiIssue)>();
        var newTodos = new List<SourceReference>();

        foreach (var sourceTodo in sourceTodos)
        {
            var matchingIssue = FindMatchingIssue(sourceTodo, existingIssues);
            if (matchingIssue != null)
            {
                matchedIssues.Add((sourceTodo, matchingIssue));
            }
            else
            {
                newTodos.Add(sourceTodo);
            }
        }

        // Create new issues for unmatched TODOs
        var newIssues = new List<GatheredIssue>();
        var filesModified = new List<string>();

        if (!dryRun)
        {
            foreach (var newTodo in newTodos)
            {
                var uuid = Issue.GenerateUuid();
                var issue = CreateIssueFromTodo(newTodo, uuid);
                newIssues.Add(issue);

                // Insert UUID reference into source file
                var rewriteResult = _fileRewriter.InsertUuidReference(newTodo.FilePath, newTodo, uuid);
                if (rewriteResult.Success)
                {
                    filesModified.Add(rewriteResult.FilePath);
                }
            }

            // Save new issues to org file
            if (newIssues.Any())
            {
                SaveIssuesToOrgFile(orgFilePath, newIssues);
            }
        }

        return new GatherResult
        {
            SourceFilesScanned = sourceFiles.Count,
            TodosFound = sourceTodos.Count,
            ExistingIssuesFound = existingIssues.Count,
            NewIssuesCreated = newIssues.Count,
            FilesModified = filesModified.Distinct().Count(),
            MatchedIssues = matchedIssues.Count,
            DryRun = dryRun
        };
    }

    /// <summary>
    /// Syncs orgi issue status back to source files (removes completed TODOs)
    /// </summary>
    public SyncResult SyncToSource(string orgFilePath, bool autoConfirm = false)
    {
        if (!File.Exists(orgFilePath))
        {
            throw new FileNotFoundException($"Org file not found: {orgFilePath}");
        }

        // Parse orgi issues
        var parser = new Parser(orgFilePath);
        var allIssues = parser.Parse().ToList();

        // Filter for DONE issues with source references
        var completedSourceIssues = allIssues
            .Where(issue => issue.State == IssueState.Done && issue.HasSourceReference)
            .ToList();

        var filesModified = new List<string>();
        var todosRemoved = 0;
        var todosSkipped = 0;

        foreach (var issue in completedSourceIssues)
        {
            if (string.IsNullOrEmpty(issue.SourceFile) || !issue.SourceLine.HasValue)
            {
                continue;
            }

            var sourceFilePath = ResolveSourceFilePath(issue.SourceFile!);
            if (!File.Exists(sourceFilePath))
            {
                Console.WriteLine($"Warning: Source file not found: {sourceFilePath}");
                todosSkipped++;
                continue;
            }

            // Ask user for confirmation unless auto-confirm is enabled
            if (!autoConfirm)
            {
                Console.WriteLine($"\nRemove TODO from {issue.SourceFile}:{issue.SourceLine}?");
                Console.WriteLine($"Issue: {issue.Title}");
                Console.Write("Remove? (y/N/a=remove all): ");

                var response = Console.ReadLine()?.ToLowerInvariant();
                if (response == "a")
                {
                    autoConfirm = true; // Remove all remaining
                }
                else if (response != "y")
                {
                    todosSkipped++;
                    continue;
                }
            }

            // Remove the TODO line
            var rewriteResult = _fileRewriter.RemoveTodoLine(sourceFilePath, issue.SourceLine!.Value);
            if (rewriteResult.Success)
            {
                filesModified.Add(sourceFilePath);
                todosRemoved++;
                Console.WriteLine($"✓ Removed TODO from {sourceFilePath}:{issue.SourceLine}");
            }
            else
            {
                Console.WriteLine($"✗ Failed to remove TODO: {rewriteResult.Message}");
                todosSkipped++;
            }
        }

        // Update org file to remove source references from completed issues
        if (todosRemoved > 0)
        {
            UpdateOrgFileRemoveSourceReferences(orgFilePath, completedSourceIssues);
        }

        return new SyncResult
        {
            TotalIssuesChecked = allIssues.Count,
            CompletedSourceIssuesFound = completedSourceIssues.Count,
            TodosRemoved = todosRemoved,
            TodosSkipped = todosSkipped,
            FilesModified = filesModified.Distinct().Count()
        };
    }

    /// <summary>
    /// Validates that source TODOs are still synchronized with orgi issues
    /// </summary>
    public ValidationResult ValidateSync(string sourceDirectory, string orgFilePath)
    {
        if (!Directory.Exists(sourceDirectory) || !File.Exists(orgFilePath))
        {
            throw new ArgumentException("Source directory and org file must exist");
        }

        // Get current source TODOs
        var sourceFiles = _fileDiscovery.DiscoverFiles(sourceDirectory);
        var currentSourceTodos = _todoParser.ExtractTodos(sourceFiles).ToList();

        // Get orgi issues with source references
        var parser = new Parser(orgFilePath);
        var sourceIssues = parser.Parse()
            .Where(issue => issue.HasSourceReference)
            .ToList();

        var issues = new List<ValidationIssue>();

        // Check for TODOs that were removed from source but still exist in orgi
        foreach (var orgiIssue in sourceIssues)
        {
            var matchingSourceTodo = currentSourceTodos.FirstOrDefault(st =>
                st.RelativePath.Equals(orgiIssue.SourceFile, StringComparison.OrdinalIgnoreCase) &&
                st.LineNumber == orgiIssue.SourceLine);

            if (matchingSourceTodo == null)
            {
                issues.Add(new ValidationIssue
                {
                    Type = ValidationIssueType.SourceTodoMissing,
                    Issue = orgiIssue,
                    Message = $"TODO was removed from source file but issue still exists in orgi"
                });
            }
        }

        // Check for TODOs that exist in source but not in orgi
        foreach (var sourceTodo in currentSourceTodos)
        {
            var matchingOrgiIssue = sourceIssues.FirstOrDefault(issue =>
                issue.SourceFile?.Equals(sourceTodo.RelativePath, StringComparison.OrdinalIgnoreCase) == true &&
                issue.SourceLine == sourceTodo.LineNumber);

            if (matchingOrgiIssue == null)
            {
                issues.Add(new ValidationIssue
                {
                    Type = ValidationIssueType.OrgiIssueMissing,
                    SourceTodo = sourceTodo,
                    Message = $"TODO exists in source but no corresponding orgi issue found"
                });
            }
        }

        return new ValidationResult
        {
            Issues = issues,
            IsValid = !issues.Any()
        };
    }

    private IEnumerable<Issue> LoadExistingIssues(string orgFilePath)
    {
        if (!File.Exists(orgFilePath))
        {
            return Enumerable.Empty<Issue>();
        }

        try
        {
            var parser = new Parser(orgFilePath);
            return parser.Parse();
        }
        catch
        {
            return Enumerable.Empty<Issue>();
        }
    }

    private Issue? FindMatchingIssue(SourceReference sourceTodo, IEnumerable<Issue> existingIssues)
    {
        return existingIssues.FirstOrDefault(issue =>
            issue.HasSourceReference &&
            issue.SourceFile?.Equals(sourceTodo.RelativePath, StringComparison.OrdinalIgnoreCase) == true &&
            issue.SourceLine == sourceTodo.LineNumber);
    }

    private GatheredIssue CreateIssueFromTodo(SourceReference sourceTodo, string uuid)
    {
        var now = DateTime.Now;
        var title = sourceTodo.TodoText.Length > 50
            ? sourceTodo.TodoText[..47] + "..."
            : sourceTodo.TodoText;

        // Determine priority based on TODO keyword
        var priority = sourceTodo.TodoKeyword.ToUpperInvariant() switch
        {
            "FIXME" or "BUG" => Priority.A,
            "TODO" => Priority.B,
            "HACK" => Priority.C,
            _ => Priority.None
        };

        var tags = new List<string> { "gathered", sourceTodo.CommentStyle.ToLowerInvariant() };

        return new GatheredIssue(
            id: $"gather-{now:yyyyMMdd-HHmmss}-{uuid}",
            title: title,
            description: sourceTodo.FullTodoText,
            createdAt: now,
            sourceReference: sourceTodo,
            orgiUuid: uuid,
            state: IssueState.Todo,
            priority: priority,
            tags: tags
        );
    }

    private void SaveIssuesToOrgFile(string orgFilePath, IEnumerable<GatheredIssue> newIssues)
    {
        var orgLines = new List<string>();

        // Read existing content if file exists
        if (File.Exists(orgFilePath))
        {
            orgLines.AddRange(File.ReadAllLines(orgFilePath));
        }

        // Add new issues
        foreach (var issue in newIssues)
        {
            orgLines.Add($"* TODO {issue.Title}");
            orgLines.Add($"  :PROPERTIES:");
            orgLines.Add($"  :ID: {issue.Id}");
            orgLines.Add($"  :TITLE: {issue.Title}");
            orgLines.Add($"  :CREATED: <{issue.CreatedAt:yyyy-MM-dd ddd HH:mm}>");
            orgLines.Add($"  :SOURCE_FILE: {issue.SourceReference.RelativePath}");
            orgLines.Add($"  :SOURCE_LINE: {issue.SourceReference.LineNumber}");
            orgLines.Add($"  :SOURCE_COLUMN: {issue.SourceReference.ColumnNumber}");
            orgLines.Add($"  :SOURCE_UUID: {issue.SourceReference.ContentHash}");

            if (issue.Priority != Priority.None)
            {
                orgLines.Add($"  :PRIORITY: {issue.Priority}");
            }

            if (issue.Tags.Any())
            {
                orgLines.Add($"  :TAGS: {string.Join(":", issue.Tags)}");
            }

            orgLines.Add($"  :END:");
            orgLines.Add("");
            orgLines.Add($"  {issue.Description}");
            orgLines.Add("");
        }

        // Write back to file
        File.WriteAllLines(orgFilePath, orgLines);
    }

    private string ResolveSourceFilePath(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            throw new ArgumentException("Source file path is null or empty");
        }

        var currentDir = Directory.GetCurrentDirectory();
        var fullPath = Path.Combine(currentDir, relativePath);
        return Path.GetFullPath(fullPath);
    }

    private void UpdateOrgFileRemoveSourceReferences(string orgFilePath, List<Issue> completedIssues)
    {
        // This would involve updating the org file to remove source reference properties
        // For now, we'll leave the source references in the org file for historical tracking
        // In a future version, we could optionally clean these up
    }
}

/// <summary>
/// Result of a gather operation
/// </summary>
public class GatherResult
{
    public int SourceFilesScanned { get; init; }
    public int TodosFound { get; init; }
    public int ExistingIssuesFound { get; init; }
    public int NewIssuesCreated { get; init; }
    public int FilesModified { get; init; }
    public int MatchedIssues { get; init; }
    public bool DryRun { get; init; }
}

/// <summary>
/// Result of a sync operation
/// </summary>
public class SyncResult
{
    public int TotalIssuesChecked { get; init; }
    public int CompletedSourceIssuesFound { get; init; }
    public int TodosRemoved { get; init; }
    public int TodosSkipped { get; init; }
    public int FilesModified { get; init; }
}

/// <summary>
/// Result of a validation operation
/// </summary>
public class ValidationResult
{
    public List<ValidationIssue> Issues { get; init; } = new();
    public bool IsValid { get; init; }
}

/// <summary>
/// Represents a validation issue
/// </summary>
public class ValidationIssue
{
    public ValidationIssueType Type { get; init; }
    public Issue? Issue { get; init; }
    public SourceReference? SourceTodo { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Types of validation issues
/// </summary>
public enum ValidationIssueType
{
    SourceTodoMissing,
    OrgiIssueMissing,
    ContentMismatch
}