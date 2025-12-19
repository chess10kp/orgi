using Orgi.Core.Discovery;
using Orgi.Core.Model;
using Orgi.Core.Parsing;
using Orgi.Core.Rewriting;
using Orgi.Core.Sync;
using Xunit;

namespace Orgi.Core.Tests.GatherFunctionality;

public class GatherIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _orgFile;

    public GatherIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _orgFile = Path.Combine(_tempDir, ".orgi", "orgi.org");
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.GetDirectoryName(_orgFile)!);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void GatherFromSource_SimpleScenario_CreatesIssuesAndUpdatesSource()
    {
        // Arrange
        var sourceFile = Path.Combine(_tempDir, "Program.cs");
        var sourceContent = @"
using System;

class Program
{
    // TODO: Implement error handling
    static void Main(string[] args)
    {
        Console.WriteLine(""Hello World"");

        // FIXME: Handle empty args
        if (args.Length == 0)
        {
            return;
        }
    }
}";
        File.WriteAllText(sourceFile, sourceContent);

        var synchronizer = new IssueSynchronizer();

        // Act
        var result = synchronizer.GatherFromSource(_tempDir, _orgFile);

        // Assert
        Assert.True(result.NewIssuesCreated > 0);
        Assert.True(result.FilesModified > 0);
        Assert.Equal(2, result.TodosFound);
        Assert.Equal(1, result.FilesModified);

        // Check that source file was modified
        var modifiedContent = File.ReadAllText(sourceFile);
        Assert.Contains("[orgi:", modifiedContent);

        // Check that org file was created
        Assert.True(File.Exists(_orgFile));
        var orgContent = File.ReadAllText(_orgFile);
        Assert.Contains("Implement error handling", orgContent);
        Assert.Contains("Handle empty args", orgContent);
    }

    [Fact]
    public void GatherFromSource_ExistingOrgiFile_AppendsNewIssues()
    {
        // Arrange
        var existingContent = @"* TODO Existing issue
  :PROPERTIES:
  :ID: existing-001
  :TITLE: Existing Issue
  :CREATED: <2025-12-18 Thu 10:00>
  :END:

  This is an existing issue.";
        File.WriteAllText(_orgFile, existingContent);

        var sourceFile = Path.Combine(_tempDir, "test.cs");
        File.WriteAllText(sourceFile, "// TODO: New gathered issue");

        var synchronizer = new IssueSynchronizer();

        // Act
        var result = synchronizer.GatherFromSource(_tempDir, _orgFile);

        // Assert
        Assert.Equal(1, result.NewIssuesCreated);
        Assert.Equal(1, result.ExistingIssuesFound);

        var orgContent = File.ReadAllText(_orgFile);
        Assert.Contains("Existing issue", orgContent);
        Assert.Contains("New gathered issue", orgContent);
    }

    [Fact]
    public void GatherFromSource_AlreadyGatheredTodos_SkipsDuplicates()
    {
        // Arrange
        var sourceFile = Path.Combine(_tempDir, "test.cs");
        var sourceContent = "// TODO: [orgi:abc12345] Already gathered issue\n// TODO: New issue to gather";
        File.WriteAllText(sourceFile, sourceContent);

        var synchronizer = new IssueSynchronizer();

        // Act
        var result = synchronizer.GatherFromSource(_tempDir, _orgFile);

        // Assert
        Assert.Equal(1, result.NewIssuesCreated);
        Assert.Equal(1, result.TodosFound); // Only the new TODO should be found
    }

    [Fact]
    public void GatherFromSource_DryRun_DoesNotModifyFiles()
    {
        // Arrange
        var sourceFile = Path.Combine(_tempDir, "test.cs");
        var originalContent = "// TODO: Test TODO";
        File.WriteAllText(sourceFile, originalContent);

        var synchronizer = new IssueSynchronizer();

        // Act
        var result = synchronizer.GatherFromSource(_tempDir, _orgFile, dryRun: true);

        // Assert
        Assert.True(result.DryRun);
        Assert.Equal(1, result.TodosFound);
        Assert.Equal(0, result.NewIssuesCreated);
        Assert.Equal(0, result.FilesModified);

        // Check that files weren't modified
        var currentContent = File.ReadAllText(sourceFile);
        Assert.Equal(originalContent, currentContent);
        Assert.False(File.Exists(_orgFile));
    }

    [Fact]
    public void GatherFromSource_MultipleFileTypes_ParsesCorrectly()
    {
        // Arrange
        var files = new[]
        {
            ("Program.cs", "// TODO: C# TODO item"),
            ("script.js", "// FIXME: JavaScript fixme"),
            ("app.py", "# TODO: Python TODO item"),
            ("index.html", "<!-- TODO: HTML TODO item -->")
        };

        foreach (var (fileName, content) in files)
        {
            File.WriteAllText(Path.Combine(_tempDir, fileName), content);
        }

        var synchronizer = new IssueSynchronizer();

        // Act
        var result = synchronizer.GatherFromSource(_tempDir, _orgFile);

        // Assert
        Assert.Equal(4, result.NewIssuesCreated);
        Assert.Equal(4, result.FilesModified);

        var orgContent = File.ReadAllText(_orgFile);
        Assert.Contains("C# TODO item", orgContent);
        Assert.Contains("JavaScript fixme", orgContent);
        Assert.Contains("Python TODO item", orgContent);
        Assert.Contains("HTML TODO item", orgContent);
    }

    [Fact]
    public void SyncToSource_WithCompletedIssues_RemovesTodosFromSource()
    {
        // Arrange - Create org file with completed issues that have source references
        var orgContent = @"* TODO Regular issue
  :PROPERTIES:
  :ID: regular-001
  :TITLE: Regular Issue
  :CREATED: <2025-12-18 Thu 10:00>
  :END:

  Regular issue description.

* DONE Completed issue with source reference
  :PROPERTIES:
  :ID: completed-001
  :TITLE: Completed Issue
  :CREATED: <2025-12-18 Thu 10:00>
  :SOURCE_FILE: test.cs
  :SOURCE_LINE: 2
  :SOURCE_COLUMN: 5
  :SOURCE_UUID: 12345
  :END:

  This issue was completed.

* INPROGRESS In-progress issue
  :PROPERTIES:
  :ID: inprogress-001
  :TITLE: In-Progress Issue
  :CREATED: <2025-12-18 Thu 10:00>
  :SOURCE_FILE: test.cs
  :SOURCE_LINE: 4
  :SOURCE_COLUMN: 5
  :SOURCE_UUID: 67890
  :END:

  This issue is in progress.";
        File.WriteAllText(_orgFile, orgContent);

        // Create source file with TODOs
        var sourceFile = Path.Combine(_tempDir, "test.cs");
        var sourceContent = @"using System;

// TODO: This should be removed (completed)
class Program
{
    // TODO: This should remain (in-progress)
    static void Main() { }
}";
        File.WriteAllText(sourceFile, sourceContent);

        var synchronizer = new IssueSynchronizer();

        // Act
        var result = synchronizer.SyncToSource(_orgFile, autoConfirm: true);

        // Assert
        Assert.Equal(1, result.TodosRemoved); // Only the DONE issue should be removed
        Assert.Equal(1, result.FilesModified);

        var modifiedContent = File.ReadAllText(sourceFile);
        var lines = modifiedContent.Split('\n');

        // Check that the completed TODO was removed
        var todoLine = lines.FirstOrDefault(line => line.Contains("This should be removed"));
        Assert.Null(todoLine);

        // Check that the in-progress TODO remains
        var inProgressLine = lines.FirstOrDefault(line => line.Contains("This should remain"));
        Assert.NotNull(inProgressLine);
    }

    [Fact]
    public void ValidateSync_WithInconsistentState_DetectsIssues()
    {
        // Arrange - Create source file with TODOs
        var sourceFile = Path.Combine(_tempDir, "test.cs");
        File.WriteAllText(sourceFile, "// TODO: Source TODO that exists in both");

        // Create org file without that issue
        File.WriteAllText(_orgFile, "* TODO Some other issue\n  :PROPERTIES:\n  :ID: other-001\n  :END:");

        var synchronizer = new IssueSynchronizer();

        // Act
        var result = synchronizer.ValidateSync(_tempDir, _orgFile);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal(ValidationIssueType.OrgiIssueMissing, result.Issues[0].Type);
    }

    [Fact]
    public void GatherFromSource_WithExcludedFiles_SkipsCorrectly()
    {
        // Arrange
        var files = new[]
        {
            "Program.cs",           // Should be included
            "Program.test.cs",      // Should be excluded
            "bin/compiled.cs",      // Should be excluded
            "script.js",           // Should be included
            "spec.test.js",        // Should be excluded
        };

        // Create subdirectories
        Directory.CreateDirectory(Path.Combine(_tempDir, "bin"));

        foreach (var file in files)
        {
            var fullPath = Path.Combine(_tempDir, file);
            var directory = Path.GetDirectoryName(fullPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(fullPath, "// TODO: TODO in " + file);
        }

        var synchronizer = new IssueSynchronizer();

        // Act
        var result = synchronizer.GatherFromSource(_tempDir, _orgFile);

        // Assert
        Assert.Equal(2, result.NewIssuesCreated); // Only Program.cs and script.js should be processed
    }

    [Fact]
    public void FullWorkflow_GatherThenSync_CompletesSuccessfully()
    {
        // Arrange - Create source files with TODOs
        var sourceFile1 = Path.Combine(_tempDir, "task1.cs");
        var sourceFile2 = Path.Combine(_tempDir, "task2.cs");

        File.WriteAllText(sourceFile1, "// TODO: First task to complete");
        File.WriteAllText(sourceFile2, "// TODO: Second task to complete");

        var synchronizer = new IssueSynchronizer();

        // Act 1: Gather TODOs
        var gatherResult = synchronizer.GatherFromSource(_tempDir, _orgFile);
        Assert.Equal(2, gatherResult.NewIssuesCreated);

        // Modify org file to mark one issue as DONE
        var orgContent = File.ReadAllText(_orgFile);
        var modifiedOrgContent = orgContent.Replace("* TODO", "* DONE");
        File.WriteAllText(_orgFile, modifiedOrgContent);

        // Act 2: Sync completed issues back to source
        var syncResult = synchronizer.SyncToSource(_orgFile, autoConfirm: true);
        Assert.Equal(2, syncResult.TodosRemoved);

        // Assert - Check that TODOs were removed from source files
        var source1Content = File.ReadAllText(sourceFile1);
        var source2Content = File.ReadAllText(sourceFile2);

        Assert.DoesNotContain("TODO:", source1Content);
        Assert.DoesNotContain("TODO:", source2Content);
    }

    [Fact]
    public void GatherFromSource_WithComplexTodoText_HandlesSpecialCharacters()
    {
        // Arrange
        var sourceFile = Path.Combine(_tempDir, "complex.cs");
        var complexTodo = "// TODO: Handle complex scenario: \"API endpoint https://api.example.com/v1/users?active=true returns {\"status\": \"ok\", \"data\": []}\"";
        File.WriteAllText(sourceFile, complexTodo);

        var synchronizer = new IssueSynchronizer();

        // Act
        var result = synchronizer.GatherFromSource(_tempDir, _orgFile);

        // Assert
        Assert.Equal(1, result.NewIssuesCreated);
        var orgContent = File.ReadAllText(_orgFile);
        Assert.Contains("complex scenario", orgContent);
        Assert.Contains("api.example.com", orgContent);
    }
}