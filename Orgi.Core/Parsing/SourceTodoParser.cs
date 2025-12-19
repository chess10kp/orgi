using Orgi.Core.Model;
using System.Text.RegularExpressions;

namespace Orgi.Core.Parsing;

/// <summary>
/// Parses source code files to extract TODO and FIXME items
/// </summary>
public class SourceTodoParser
{
    private static readonly Dictionary<string, CommentStyle> FileExtensionStyles = new()
    {
        [".cs"] = CommentStyles.CStyle,
        [".js"] = CommentStyles.CStyle,
        [".ts"] = CommentStyles.CStyle,
        [".jsx"] = CommentStyles.CStyle,
        [".tsx"] = CommentStyles.CStyle,
        [".java"] = CommentStyles.CStyle,
        [".cpp"] = CommentStyles.CStyle,
        [".c"] = CommentStyles.CStyle,
        [".h"] = CommentStyles.CStyle,
        [".hpp"] = CommentStyles.CStyle,
        [".go"] = CommentStyles.CStyle,
        [".rs"] = CommentStyles.CStyle,
        [".swift"] = CommentStyles.CStyle,
        [".dart"] = CommentStyles.CStyle,
        [".py"] = CommentStyles.Python,
        [".rb"] = CommentStyles.Ruby,
        [".php"] = CommentStyles.PHP,
        [".sh"] = CommentStyles.Shell,
        [".bash"] = CommentStyles.Shell,
        [".ps1"] = CommentStyles.PowerShell,
        [".lua"] = CommentStyles.Lua,
        [".sql"] = CommentStyles.SQL,
        [".pl"] = CommentStyles.Perl,
        [".scala"] = CommentStyles.CStyle,
        [".kt"] = CommentStyles.CStyle,
        [".html"] = CommentStyles.HTML,
        [".xml"] = CommentStyles.XML,
        [".css"] = CommentStyles.CSS,
        [".scss"] = CommentStyles.CStyle,
        [".less"] = CommentStyles.CStyle
    };

    private static readonly string[] TodoKeywords = { "TODO", "FIXME", "HACK", "BUG", "NOTE", "XXX", "REVIEW" };

    /// <summary>
    /// Represents a comment style for a programming language
    /// </summary>
    private record CommentStyle(string[] SingleLinePatterns, string? MultiLineStart, string? MultiLineEnd);

    /// <summary>
    /// Comment style definitions for different languages
    /// </summary>
    private static class CommentStyles
    {
        public static readonly CommentStyle CStyle = new(
            SingleLinePatterns: new[]
            {
                "//\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+)",
                "/\\*\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+?)\\*/"
            },
            MultiLineStart: "/*",
            MultiLineEnd: "*/"
        );

        public static readonly CommentStyle Python = new(
            SingleLinePatterns: new[]
            {
                "#\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+)",
                "'''\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+?)'''",
                "\"\"\"\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+?)\"\"\""
            },
            MultiLineStart: null,
            MultiLineEnd: null
        );

        public static readonly CommentStyle Ruby = new(
            SingleLinePatterns: new[]
            {
                "#\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+)"
            },
            MultiLineStart: "=begin",
            MultiLineEnd: "=end"
        );

        public static readonly CommentStyle PHP = new(
            SingleLinePatterns: new[]
            {
                "//\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+)",
                "#\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+)"
            },
            MultiLineStart: "/*",
            MultiLineEnd: "*/"
        );

        public static readonly CommentStyle Shell = new(
            SingleLinePatterns: new[]
            {
                "#\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+)"
            },
            MultiLineStart: null,
            MultiLineEnd: null
        );

        public static readonly CommentStyle PowerShell = new(
            SingleLinePatterns: new[]
            {
                "#\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+)",
                "<#\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+?)#>"
            },
            MultiLineStart: "<#",
            MultiLineEnd: "#>"
        );

        public static readonly CommentStyle Lua = new(
            SingleLinePatterns: new[]
            {
                "--\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+)"
            },
            MultiLineStart: "--[[",
            MultiLineEnd: "]]"
        );

        public static readonly CommentStyle SQL = new(
            SingleLinePatterns: new[]
            {
                "--\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+)"
            },
            MultiLineStart: "/*",
            MultiLineEnd: "*/"
        );

        public static readonly CommentStyle Perl = new(
            SingleLinePatterns: new[]
            {
                "#\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+)"
            },
            MultiLineStart: null,
            MultiLineEnd: null
        );

        public static readonly CommentStyle HTML = new(
            SingleLinePatterns: new[]
            {
                "<!--\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+?)-->"
            },
            MultiLineStart: "<!--",
            MultiLineEnd: "-->"
        );

        public static readonly CommentStyle XML = new(
            SingleLinePatterns: new[]
            {
                "<!--\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+?)-->"
            },
            MultiLineStart: "<!--",
            MultiLineEnd: "-->"
        );

        public static readonly CommentStyle CSS = new(
            SingleLinePatterns: new[]
            {
                "/\\*\\s*(TODO|FIXME|HACK|BUG|NOTE|XXX|REVIEW):\\s*(.+?)\\*/"
            },
            MultiLineStart: "/*",
            MultiLineEnd: "*/"
        );
    }

  
    /// <summary>
    /// Extracts TODO items from a single source file
    /// </summary>
    public IEnumerable<SourceReference> ExtractTodos(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Source file not found: {filePath}");
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!FileExtensionStyles.TryGetValue(extension, out var commentStyle))
        {
            // Default to C-style for unknown extensions
            commentStyle = CommentStyles.CStyle;
        }

        var lines = File.ReadAllLines(filePath);
        var todos = new List<SourceReference>();

        for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
        {
            var line = lines[lineNumber];
            var lineTodos = ExtractTodosFromLine(line, lineNumber + 1, filePath, commentStyle);
            todos.AddRange(lineTodos);
        }

        return todos;
    }

    /// <summary>
    /// Extracts TODO items from multiple files
    /// </summary>
    public IEnumerable<SourceReference> ExtractTodos(IEnumerable<string> filePaths)
    {
        var allTodos = new List<SourceReference>();

        foreach (var filePath in filePaths)
        {
            try
            {
                var fileTodos = ExtractTodos(filePath);
                allTodos.AddRange(fileTodos);
            }
            catch (Exception ex)
            {
                // Log error and continue with other files
                Console.WriteLine($"Warning: Failed to parse {filePath}: {ex.Message}");
            }
        }

        return allTodos;
    }

    /// <summary>
    /// Extracts TODO items from a single line
    /// </summary>
    private IEnumerable<SourceReference> ExtractTodosFromLine(string line, int lineNumber, string filePath, CommentStyle commentStyle)
    {
        var todos = new List<SourceReference>();

        // Skip TODOs that already have orgi UUID references
        if (line.Contains("[orgi:", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        foreach (var pattern in commentStyle.SingleLinePatterns)
        {
            var matches = Regex.Matches(line, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    var keyword = match.Groups[1].Value.ToUpperInvariant();
                    var text = match.Groups[2].Value.Trim();

                    // Calculate column position (1-based)
                    var column = match.Index + 1;

                    var todo = new SourceReference(
                        filePath,
                        lineNumber,
                        column,
                        line,
                        keyword,
                        text,
                        GetCommentStyleName(filePath)
                    );

                    todos.Add(todo);
                }
            }
        }

        foreach (var todo in todos)
        {
            yield return todo;
        }
    }

    /// <summary>
    /// Gets the name of the comment style for the given file
    /// </summary>
    private static string GetCommentStyleName(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return FileExtensionStyles.TryGetValue(extension, out _)
            ? extension.ToUpperInvariant()[1..]
            : "UNKNOWN";
    }
}