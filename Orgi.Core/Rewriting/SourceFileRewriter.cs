using Orgi.Core.Model;

namespace Orgi.Core.Rewriting;

/// <summary>
/// Really couldn't come up with a better name ngl
/// </summary>
public class SourceFileRewriter
{
    private readonly string _backupDirectory;

    public SourceFileRewriter(string? backupDirectory = null)
    {
        _backupDirectory = backupDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), ".orgi", "backups");
        EnsureBackupDirectoryExists();
    }

    /// <summary>
    /// Inserts a UUID reference into a source TODO line
    /// </summary>
    public RewriteResult InsertUuidReference(string filePath, SourceReference sourceRef, string uuid)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Source file not found: {filePath}");
        }

        try
        {
            // Create backup
            CreateBackup(filePath);

            // Read file lines
            var lines = File.ReadAllLines(filePath);
            var targetLineIndex = sourceRef.LineNumber - 1;

            if (targetLineIndex < 0 || targetLineIndex >= lines.Length)
            {
                throw new InvalidOperationException($"Line number {sourceRef.LineNumber} is out of range for file {filePath}");
            }

            var originalLine = lines[targetLineIndex];
            var modifiedLine = InsertUuidIntoLine(originalLine, uuid);

            // Check if the line actually changed
            if (originalLine == modifiedLine)
            {
                return new RewriteResult(filePath, false, "Line already contains UUID reference or no TODO found");
            }

            // Update the line
            lines[targetLineIndex] = modifiedLine;

            // Write file back
            File.WriteAllLines(filePath, lines);

            return new RewriteResult(filePath, true, $"Inserted UUID {uuid} into TODO");
        }
        catch (Exception ex)
        {
            return new RewriteResult(filePath, false, $"Failed to insert UUID: {ex.Message}");
        }
    }

    public RewriteResult RemoveTodoLine(string filePath, int lineNumber)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Source file not found: {filePath}");
        }

        try
        {
            // Create backup
            CreateBackup(filePath);

            // Read file lines
            var lines = File.ReadAllLines(filePath);
            var targetLineIndex = lineNumber - 1;

            if (targetLineIndex < 0 || targetLineIndex >= lines.Length)
            {
                throw new InvalidOperationException($"Line number {lineNumber} is out of range for file {filePath}");
            }

            var removedLine = lines[targetLineIndex];

            // Remove the line
            var newLines = lines.Where((line, index) => index != targetLineIndex).ToArray();

            // Write file back
            File.WriteAllLines(filePath, newLines);

            return new RewriteResult(filePath, true, $"Removed TODO line: {removedLine.Trim()}");
        }
        catch (Exception ex)
        {
            return new RewriteResult(filePath, false, $"Failed to remove line: {ex.Message}");
        }
    }

    /// <summary>
    /// Comments out a TODO line instead of removing it
    /// </summary>
    public RewriteResult CommentOutTodoLine(string filePath, int lineNumber, string comment = "Marked as done via orgi")
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Source file not found: {filePath}");
        }

        try
        {
            // Create backup
            CreateBackup(filePath);

            // Read file lines
            var lines = File.ReadAllLines(filePath);
            var targetLineIndex = lineNumber - 1;

            if (targetLineIndex < 0 || targetLineIndex >= lines.Length)
            {
                throw new InvalidOperationException($"Line number {lineNumber} is out of range for file {filePath}");
            }

            var originalLine = lines[targetLineIndex];
            var indentedLine = originalLine.TrimStart();
            var indent = originalLine[..(originalLine.Length - indentedLine.Length)];

            // Determine comment prefix based on file extension
            var commentPrefix = GetCommentPrefix(filePath);
            var commentedLine = $"{indent}{commentPrefix} {originalLine.Trim()} // {comment}";

            // Update the line
            lines[targetLineIndex] = commentedLine;

            // Write file back
            File.WriteAllLines(filePath, lines);

            return new RewriteResult(filePath, true, $"Commented out TODO line: {originalLine.Trim()}");
        }
        catch (Exception ex)
        {
            return new RewriteResult(filePath, false, $"Failed to comment out line: {ex.Message}");
        }
    }

    /// <summary>
    /// Inserts a UUID into a TODO line in the correct format
    /// </summary>
    private string InsertUuidIntoLine(string line, string uuid)
    {
        // Look for TODO keyword patterns and insert UUID after it
        var patterns = new[]
        {
            @"(?i)(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\s*(.+)",
            @"(?i)//\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\s*(.+)",
            @"(?i)#\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\s*(.+)",
            @"(?i)--\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\s*(.+)",
            @"(?i)/\*\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\s*(.+?)\*/",
            @"(?i)<!--\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\s*(.+?)-->"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(line, pattern);
            if (match.Success && match.Groups.Count >= 2)
            {
                var keyword = match.Groups[1].Value;
                var text = match.Groups[2].Value.Trim();

                // Insert UUID in the format: TODO: [orgi:uuid] description
                var uuidInsert = $"[orgi:{uuid}] ";
                var newLine = line.Replace(match.Value, $"{keyword}: {uuidInsert}{text}");
                return newLine;
            }
        }

        // If no pattern matched, return original line
        return line;
    }

    /// <summary>
    /// Gets the appropriate comment prefix for the file type
    /// </summary>
    private string GetCommentPrefix(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".cs" or ".js" or ".ts" or ".java" or ".cpp" or ".c" or ".h" or ".hpp" or ".go" or ".rs" or ".swift" or ".dart" or ".scala" or ".kt" => "//",
            ".py" or ".rb" or ".sh" or ".bash" or ".pl" or ".lua" => "#",
            ".sql" => "--",
            ".html" or ".xml" or ".css" => "<!--",
            ".ps1" => "#",
            _ => "//" // Default to C-style comments
        };
    }

    /// <summary>
    /// Creates a backup of the file
    /// </summary>
    private void CreateBackup(string filePath)
    {
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }

        var fileName = Path.GetFileName(filePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var backupFileName = $"{fileName}.backup-{timestamp}";
        var backupPath = Path.Combine(_backupDirectory, backupFileName);

        File.Copy(filePath, backupPath, overwrite: true);

        // Clean up old backups (keep last 10)
        CleanupOldBackups(fileName);
    }

    /// <summary>
    /// Removes old backup files, keeping only the most recent ones
    /// </summary>
    private void CleanupOldBackups(string fileName)
    {
        if (!Directory.Exists(_backupDirectory))
            return;

        var backupPattern = $"{fileName}.backup-*";
        var backupFiles = Directory.GetFiles(_backupDirectory, backupPattern)
            .OrderByDescending(f => f)
            .Skip(10); // Keep only the 10 most recent

        foreach (var oldBackup in backupFiles)
        {
            try
            {
                File.Delete(oldBackup);
            }
            catch
            {
                // Ignore errors when cleaning up old backups
            }
        }
    }

    /// <summary>
    /// Ensures the backup directory exists
    /// </summary>
    private void EnsureBackupDirectoryExists()
    {
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }
}

/// <summary>
/// Result of a file rewrite operation
/// </summary>
public record RewriteResult(string FilePath, bool Success, string Message);
