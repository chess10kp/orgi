namespace Orgi.Core.Discovery;

/// <summary>
/// Discovers source code files for TODO gathering
/// </summary>
public class SourceFileDiscovery
{
    private static readonly string[] DefaultIncludeExtensions =
    {
        ".cs", ".js", ".ts", ".jsx", ".tsx", ".py", ".java",
        ".cpp", ".c", ".h", ".hpp", ".go", ".rs", ".rb", ".php",
        ".scala", ".kt", ".swift", ".dart", ".lua", ".sh", ".ps1"
    };

    private static readonly string[] DefaultExcludePatterns =
    {
        "bin/**", "obj/**", "node_modules/**", "dist/**", "build/**",
        "target/**", ".git/**", ".svn/**", "**/*.test.*", "**/*.spec.*",
        "**/tests/**", "**/Test/**", "**/Packages/**", "**/vendor/**",
        "**/.vs/**", "**/.vscode/**"
    };

    /// <summary>
    /// Discovers source files in the specified directory
    /// </summary>
    public IEnumerable<string> DiscoverFiles(string directoryPath, IEnumerable<string>? includeExtensions = null, IEnumerable<string>? excludePatterns = null)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var includeExts = includeExtensions?.ToArray() ?? DefaultIncludeExtensions;
        var excludePatternsList = excludePatterns?.ToArray() ?? DefaultExcludePatterns;

        var allFiles = Enumerable.Empty<string>();

        try
        {
            // Get all files in current directory only (non-recursive as specified)
            allFiles = Directory.EnumerateFiles(directoryPath, "*", SearchOption.TopDirectoryOnly);
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
            return Enumerable.Empty<string>();
        }

        // Filter by file extension
        var filteredFiles = allFiles.Where(file =>
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            return includeExts.Contains(ext);
        });

        // Apply exclusion patterns
        filteredFiles = filteredFiles.Where(file =>
        {
            var relativePath = GetRelativePath(directoryPath, file);
            return !IsExcluded(relativePath, excludePatternsList);
        });

        return filteredFiles.OrderBy(f => f);
    }

    /// <summary>
    /// Gets a relative path from base directory to file
    /// </summary>
    private static string GetRelativePath(string basePath, string filePath)
    {
        if (filePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            return filePath[basePath.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        return filePath;
    }

    /// <summary>
    /// Checks if a file path matches any exclusion patterns
    /// </summary>
    private static bool IsExcluded(string relativePath, string[] excludePatterns)
    {
        foreach (var pattern in excludePatterns)
        {
            if (MatchesPattern(relativePath, pattern))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Simple pattern matching supporting wildcards
    /// </summary>
    private static bool MatchesPattern(string path, string pattern)
    {
        // Convert path separators to forward slashes for consistent pattern matching
        var normalizedPath = path.Replace('\\', '/');
        var normalizedPattern = pattern.Replace('\\', '/');

        // Split pattern and path into segments
        var patternSegments = normalizedPattern.Split('/');
        var pathSegments = normalizedPath.Split('/');

        // Check each pattern segment against the path segments
        for (int i = 0; i < patternSegments.Length; i++)
        {
            if (i >= pathSegments.Length)
            {
                // Pattern has more segments than path, check if remaining pattern segments are wildcards
                return patternSegments[i] == "**";
            }

            var patternSeg = patternSegments[i];
            var pathSeg = pathSegments[i];

            if (patternSeg == "**")
            {
                // Double wildcard matches any number of segments
                if (i == patternSegments.Length - 1)
                {
                    return true; // ** at end matches everything
                }

                // Try to match remaining pattern segments
                for (int j = i; j < pathSegments.Length; j++)
                {
                    var remainingPath = string.Join("/", pathSegments[j..]);
                    var remainingPattern = string.Join("/", patternSegments[(i + 1)..]);
                    if (MatchesPattern(remainingPath, remainingPattern))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (patternSeg == "*")
            {
                // Single wildcard matches one segment
                continue;
            }
            else if (!patternSeg.Equals(pathSeg, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // All pattern segments matched
        return true;
    }
}