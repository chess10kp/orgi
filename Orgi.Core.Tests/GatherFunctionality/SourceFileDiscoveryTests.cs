using Orgi.Core.Discovery;
using Xunit;

namespace Orgi.Core.Tests.GatherFunctionality;

public class SourceFileDiscoveryTests
{
    private readonly string _tempDir;

    public SourceFileDiscoveryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void DiscoverFiles_WithDefaultExtensions_ReturnsMatchingFiles()
    {
        // Arrange
        var sourceFiles = new[]
        {
            "Program.cs", "app.js", "script.ts", "main.py", "App.java",
            "README.md", "config.json", "data.txt"
        };

        foreach (var file in sourceFiles)
        {
            File.WriteAllText(Path.Combine(_tempDir, file), $"// Content of {file}");
        }

        var discovery = new SourceFileDiscovery();

        // Act
        var result = discovery.DiscoverFiles(_tempDir).ToList();

        // Assert
        Assert.Equal(5, result.Count); // Should find .cs, .js, .ts, .py, .java files
        Assert.Contains(result, f => f.EndsWith("Program.cs"));
        Assert.Contains(result, f => f.EndsWith("app.js"));
        Assert.Contains(result, f => f.EndsWith("script.ts"));
        Assert.Contains(result, f => f.EndsWith("main.py"));
        Assert.Contains(result, f => f.EndsWith("App.java"));
        Assert.DoesNotContain(result, f => f.EndsWith("README.md"));
        Assert.DoesNotContain(result, f => f.EndsWith("config.json"));
        Assert.DoesNotContain(result, f => f.EndsWith("data.txt"));
    }

    [Fact]
    public void DiscoverFiles_WithCustomExtensions_ReturnsOnlyMatchingFiles()
    {
        // Arrange
        var sourceFiles = new[]
        {
            "Program.cs", "app.js", "script.py", "test.rb", "config.go"
        };

        foreach (var file in sourceFiles)
        {
            File.WriteAllText(Path.Combine(_tempDir, file), $"// Content of {file}");
        }

        var discovery = new SourceFileDiscovery();
        var customExtensions = new[] { ".cs", ".py", ".go" };

        // Act
        var result = discovery.DiscoverFiles(_tempDir, customExtensions).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, f => f.EndsWith("Program.cs"));
        Assert.Contains(result, f => f.EndsWith("script.py"));
        Assert.Contains(result, f => f.EndsWith("config.go"));
        Assert.DoesNotContain(result, f => f.EndsWith("app.js"));
        Assert.DoesNotContain(result, f => f.EndsWith("test.rb"));
    }

    [Fact]
    public void DiscoverFiles_WithDefaultExcludePatterns_SkipsExcludedFiles()
    {
        // Arrange
        var sourceFiles = new[]
        {
            "Program.cs", "app.test.cs", "spec.test.js", "bin/compiled.cs",
            "obj/generated.js", "node_modules/package.js"
        };

        // Create subdirectories
        Directory.CreateDirectory(Path.Combine(_tempDir, "bin"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "obj"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "node_modules"));

        foreach (var file in sourceFiles)
        {
            var fullPath = Path.Combine(_tempDir, file);
            var directory = Path.GetDirectoryName(fullPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(fullPath, $"// Content of {file}");
        }

        var discovery = new SourceFileDiscovery();

        // Act
        var result = discovery.DiscoverFiles(_tempDir).ToList();

        // Assert
        Assert.Single(result); // Only Program.cs should remain
        Assert.Contains(result, f => f.EndsWith("Program.cs"));
        Assert.DoesNotContain(result, f => f.Contains("test."));
        Assert.DoesNotContain(result, f => f.Contains("spec."));
        Assert.DoesNotContain(result, f => f.Contains("bin/"));
        Assert.DoesNotContain(result, f => f.Contains("obj/"));
        Assert.DoesNotContain(result, f => f.Contains("node_modules/"));
    }

    [Fact]
    public void DiscoverFiles_WithCustomExcludePatterns_SkipsMatchingFiles()
    {
        // Arrange
        var sourceFiles = new[]
        {
            "Program.cs", "app.dev.cs", "config.prod.cs", "script.temp.js"
        };

        foreach (var file in sourceFiles)
        {
            File.WriteAllText(Path.Combine(_tempDir, file), $"// Content of {file}");
        }

        var discovery = new SourceFileDiscovery();
        var excludePatterns = new[] { "*.dev.*", "*.temp.*" };

        // Act
        var result = discovery.DiscoverFiles(_tempDir, excludePatterns: excludePatterns).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.EndsWith("Program.cs"));
        Assert.Contains(result, f => f.EndsWith("config.prod.cs"));
        Assert.DoesNotContain(result, f => f.EndsWith("app.dev.cs"));
        Assert.DoesNotContain(result, f => f.EndsWith("script.temp.js"));
    }

    [Fact]
    public void DiscoverFiles_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_tempDir, "nonexistent");
        var discovery = new SourceFileDiscovery();

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() =>
            discovery.DiscoverFiles(nonExistentDir).ToList());
    }

    [Fact]
    public void DiscoverFiles_EmptyDirectory_ReturnsEmptyList()
    {
        // Arrange
        var discovery = new SourceFileDiscovery();

        // Act
        var result = discovery.DiscoverFiles(_tempDir).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("script.js", ".js", true)]
    [InlineData("SCRIPT.JS", ".js", true)]
    [InlineData("script.JS", ".js", true)]
    [InlineData("script.jsx", ".js", false)]
    [InlineData("script.min.js", ".js", true)]
    [InlineData("script.ts", ".js", false)]
    [InlineData("script.js.backup", ".js", true)]
    public void DiscoverFiles_CaseInsensitiveExtensions_MatchesCorrectly(string fileName, string extension, bool shouldMatch)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, fileName), "// Content");
        var discovery = new SourceFileDiscovery();
        var extensions = new[] { extension };

        // Act
        var result = discovery.DiscoverFiles(_tempDir, extensions).ToList();

        // Assert
        if (shouldMatch)
        {
            Assert.Single(result);
            Assert.Contains(result, f => f.EndsWith(fileName));
        }
        else
        {
            Assert.Empty(result);
        }
    }

    [Fact]
    public void DiscoverFiles_WithSpecialCharactersInPaths_HandlesCorrectly()
    {
        // Arrange
        var specialFiles = new[]
        {
            "file with spaces.cs", "file-with-dashes.js", "file_with_underscores.py",
            "file.with.dots.go", "file(with)brackets.rs"
        };

        foreach (var file in specialFiles)
        {
            File.WriteAllText(Path.Combine(_tempDir, file), $"// Content of {file}");
        }

        var discovery = new SourceFileDiscovery();

        // Act
        var result = discovery.DiscoverFiles(_tempDir).ToList();

        // Assert
        Assert.Equal(specialFiles.Length, result.Count);
        foreach (var file in specialFiles)
        {
            Assert.Contains(result, f => f.EndsWith(file));
        }
    }
}