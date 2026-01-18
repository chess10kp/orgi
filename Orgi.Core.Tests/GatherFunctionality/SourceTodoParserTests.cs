using Orgi.Core.Model;
using Orgi.Core.Parsing;
using Xunit;

namespace Orgi.Core.Tests.GatherFunctionality;

public class SourceTodoParserTests
{
    private readonly string _tempDir;

    public SourceTodoParserTests()
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
    public void ExtractTodos_CStyleComments_ParsesCorrectly()
    {
        // Arrange
        var content = @"
using System;

class Program
{
    // TODO: Implement the main method
    // FIXME: This is a bug
    static void Main(string[] args)
    {
        // HACK: Temporary workaround
        Console.WriteLine(""Hello World"");
        // NOTE: Important information here
    }

    /* TODO: Multi-line comment
       TODO: Another multi-line TODO */
}";
        var filePath = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(filePath, content);
        var parser = new SourceTodoParser();

        // Act
        var result = parser.ExtractTodos(filePath).ToList();

        // Assert
        Assert.Equal(5, result.Count);

        var todo = result.FirstOrDefault(r => r.TodoKeyword == "TODO");
        Assert.NotNull(todo);
        Assert.Equal("Implement the main method", todo!.TodoText);
        Assert.Equal(4, todo.LineNumber);

        var fixme = result.FirstOrDefault(r => r.TodoKeyword == "FIXME");
        Assert.NotNull(fixme);
        Assert.Equal("This is a bug", fixme!.TodoText);

        var hack = result.FirstOrDefault(r => r.TodoKeyword == "HACK");
        Assert.NotNull(hack);
        Assert.Equal("Temporary workaround", hack!.TodoText);

        var note = result.FirstOrDefault(r => r.TodoKeyword == "NOTE");
        Assert.NotNull(note);
        Assert.Equal("Important information here", note!.TodoText);

        var multilineTodos = result.Where(r => r.LineNumber > 12).ToList();
        Assert.Equal(2, multilineTodos.Count); // Multi-line comment TODOs
    }

    [Fact]
    public void ExtractTodos_PythonComments_ParsesCorrectly()
    {
        // Arrange
        var content = @"
def main():
    # TODO: Add error handling
    print('Hello')

    # FIXME: This doesn't work correctly
    # HACK: Quick fix for production
    result = some_function()

def some_function():
    # NOTE: This function needs optimization
    pass

'''' TODO: Multi-line docstring TODO
TODO: Another docstring TODO
'''";
        var filePath = Path.Combine(_tempDir, "script.py");
        File.WriteAllText(filePath, content);
        var parser = new SourceTodoParser();

        // Act
        var result = parser.ExtractTodos(filePath).ToList();

        // Assert
        Assert.Equal(5, result.Count);

        var todos = result.Where(r => r.TodoKeyword == "TODO").ToList();
        Assert.Equal(3, todos.Count);
        Assert.Contains(todos, t => t.TodoText == "Add error handling");
        Assert.Contains(todos, t => t.TodoText == "Multi-line docstring TODO");
        Assert.Contains(todos, t => t.TodoText == "Another docstring TODO");

        var fixme = result.FirstOrDefault(r => r.TodoKeyword == "FIXME");
        Assert.NotNull(fixme);
        Assert.Equal("This doesn't work correctly", fixme!.TodoText);

        var hack = result.FirstOrDefault(r => r.TodoKeyword == "HACK");
        Assert.NotNull(hack);
        Assert.Equal("Quick fix for production", hack!.TodoText);

        var note = result.FirstOrDefault(r => r.TodoKeyword == "NOTE");
        Assert.NotNull(note);
        Assert.Equal("This function needs optimization", note!.TodoText);
    }

    [Fact]
    public void ExtractTodos_HtmlComments_ParsesCorrectly()
    {
        // Arrange
        var content = @"
<!DOCTYPE html>
<html>
<head>
    <!-- TODO: Add meta tags -->
    <title>Test Page</title>
</head>
<body>
    <!-- FIXME: Layout is broken -->
    <div class='container'>
        <!-- NOTE: Add accessibility attributes -->
        <p>Hello World</p>
    </div>
</body>
</html>";
        var filePath = Path.Combine(_tempDir, "index.html");
        File.WriteAllText(filePath, content);
        var parser = new SourceTodoParser();

        // Act
        var result = parser.ExtractTodos(filePath).ToList();

        // Assert
        Assert.Equal(3, result.Count);

        var todo = result.FirstOrDefault(r => r.TodoKeyword == "TODO");
        Assert.NotNull(todo);
        Assert.Equal("Add meta tags", todo!.TodoText);

        var fixme = result.FirstOrDefault(r => r.TodoKeyword == "FIXME");
        Assert.NotNull(fixme);
        Assert.Equal("Layout is broken", fixme!.TodoText);

        var note = result.FirstOrDefault(r => r.TodoKeyword == "NOTE");
        Assert.NotNull(note);
        Assert.Equal("Add accessibility attributes", note!.TodoText);
    }

    [Fact]
    public void ExtractTodos_JavascriptMultipleFormats_ParsesCorrectly()
    {
        // Arrange
        var content = @"
// TODO: Single line comment
function test() {
    // FIXME: Bug in this function

    /* TODO: Multi-line comment
       spanning multiple lines */

    // HACK: Workaround needed

    /* REVIEW: Code review required */
}
";
        var filePath = Path.Combine(_tempDir, "script.js");
        File.WriteAllText(filePath, content);
        var parser = new SourceTodoParser();

        // Act
        var result = parser.ExtractTodos(filePath).ToList();

        // Assert
        Assert.Equal(4, result.Count);

        var todos = result.Where(r => r.TodoKeyword == "TODO").ToList();
        Assert.Equal(2, todos.Count);
        Assert.Contains(todos, t => t.TodoText == "Single line comment");
        Assert.Contains(todos, t => t.TodoText.Contains("Multi-line comment"));

        var fixme = result.FirstOrDefault(r => r.TodoKeyword == "FIXME");
        Assert.NotNull(fixme);
        Assert.Equal("Bug in this function", fixme!.TodoText);

        var hack = result.FirstOrDefault(r => r.TodoKeyword == "HACK");
        Assert.NotNull(hack);
        Assert.Equal("Workaround needed", hack!.TodoText);

        var review = result.FirstOrDefault(r => r.TodoKeyword == "REVIEW");
        Assert.NotNull(review);
        Assert.Equal("Code review required", review!.TodoText);
    }

    [Fact]
    public void ExtractTodos_WithExistingOrgiUuid_SkipsThoseTodos()
    {
        // Arrange
        var content = @"
// TODO: [orgi:12345678] This TODO has orgi reference
// TODO: This TODO needs orgi reference
// FIXME: [orgi:87654321] This FIXME has orgi reference
// FIXME: This FIXME needs orgi reference
";
        var filePath = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(filePath, content);
        var parser = new SourceTodoParser();

        // Act
        var result = parser.ExtractTodos(filePath).ToList();

        // Assert
        Assert.Equal(2, result.Count); // Should skip the ones with [orgi:uuid]

        var todo = result.FirstOrDefault(r => r.TodoKeyword == "TODO");
        Assert.NotNull(todo);
        Assert.Equal("This TODO needs orgi reference", todo!.TodoText);

        var fixme = result.FirstOrDefault(r => r.TodoKeyword == "FIXME");
        Assert.NotNull(fixme);
        Assert.Equal("This FIXME needs orgi reference", fixme!.TodoText);
    }

    [Fact]
    public void ExtractTodos_VariousTodoKeywords_ParsesCorrectly()
    {
        // Arrange
        var content = @"
// TODO: Regular todo item
// FIXME: Bug that needs fixing
// HACK: Temporary hack
// BUG: Known bug
// NOTE: Important note
// XXX: Code smell
// REVIEW: Needs review
";
        var filePath = Path.Combine(_tempDir, "test.cs");
        File.WriteAllText(filePath, content);
        var parser = new SourceTodoParser();

        // Act
        var result = parser.ExtractTodos(filePath).ToList();

        // Assert
        Assert.Equal(7, result.Count);
        Assert.Contains(result, r => r.TodoKeyword == "TODO" && r.TodoText == "Regular todo item");
        Assert.Contains(result, r => r.TodoKeyword == "FIXME" && r.TodoText == "Bug that needs fixing");
        Assert.Contains(result, r => r.TodoKeyword == "HACK" && r.TodoText == "Temporary hack");
        Assert.Contains(result, r => r.TodoKeyword == "BUG" && r.TodoText == "Known bug");
        Assert.Contains(result, r => r.TodoKeyword == "NOTE" && r.TodoText == "Important note");
        Assert.Contains(result, r => r.TodoKeyword == "XXX" && r.TodoText == "Code smell");
        Assert.Contains(result, r => r.TodoKeyword == "REVIEW" && r.TodoText == "Needs review");
    }

    [Theory]
    [InlineData("Program.cs", "CS")]
    [InlineData("script.js", "JS")]
    [InlineData("app.ts", "TS")]
    [InlineData("main.py", "PY")]
    [InlineData("App.java", "JAVA")]
    [InlineData("script.sh", "SH")]
    [InlineData("index.html", "HTML")]
    public void ExtractTodos_DifferentFileExtensions_SetsCorrectCommentStyle(string fileName, string expectedStyle)
    {
        // Arrange
        var content = $"// TODO: Test todo for {fileName}";
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, content);
        var parser = new SourceTodoParser();

        // Act
        var result = parser.ExtractTodos(filePath).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedStyle, result[0].CommentStyle);
    }

    [Fact]
    public void ExtractTodos_EmptyFile_ReturnsEmptyList()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "empty.cs");
        File.WriteAllText(filePath, "");
        var parser = new SourceTodoParser();

        // Act
        var result = parser.ExtractTodos(filePath).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractTodos_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_tempDir, "nonexistent.cs");
        var parser = new SourceTodoParser();

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() =>
            parser.ExtractTodos(nonExistentFile).ToList());
    }

    [Fact]
    public void ExtractTodos_WithIndentation_PreservesColumnInformation()
    {
        // Arrange
        var content = @"
        // TODO: Indented todo
    // FIXME: Less indented fixme
  // HACK: Even less indented hack
";
        var filePath = Path.Combine(_tempDir, "indented.cs");
        File.WriteAllText(filePath, content);
        var parser = new SourceTodoParser();

        // Act
        var result = parser.ExtractTodos(filePath).ToList();

        // Assert
        Assert.Equal(3, result.Count);

        var todo = result.FirstOrDefault(r => r.TodoKeyword == "TODO");
        Assert.NotNull(todo);
        Assert.True(todo!.ColumnNumber > 8); // Should account for indentation

        var fixme = result.FirstOrDefault(r => r.TodoKeyword == "FIXME");
        Assert.NotNull(fixme);
        Assert.True(fixme!.ColumnNumber > 4); // Should account for less indentation

        var hack = result.FirstOrDefault(r => r.TodoKeyword == "HACK");
        Assert.NotNull(hack);
        Assert.True(hack!.ColumnNumber > 2); // Should account for even less indentation
    }

    [Fact]
    public void ExtractTodos_MultipleFiles_ParsesAllFiles()
    {
        // Arrange
        var files = new[]
        {
            ("file1.cs", "// TODO: Todo in file 1"),
            ("file2.cs", "// FIXME: Fixme in file 2"),
            ("file3.cs", "// HACK: Hack in file 3")
        };

        var filePaths = new List<string>();
        foreach (var (fileName, content) in files)
        {
            var filePath = Path.Combine(_tempDir, fileName);
            File.WriteAllText(filePath, content);
            filePaths.Add(filePath);
        }

        var parser = new SourceTodoParser();

        // Act
        var result = parser.ExtractTodos(filePaths).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, r => r.TodoKeyword == "TODO" && r.TodoText == "Todo in file 1");
        Assert.Contains(result, r => r.TodoKeyword == "FIXME" && r.TodoText == "Fixme in file 2");
        Assert.Contains(result, r => r.TodoKeyword == "HACK" && r.TodoText == "Hack in file 3");
    }

    [Fact]
    public void ExtractTodos_WithSpecialCharactersInTodoText_ParsesCorrectly()
    {
        // Arrange
        var content = @"
// TODO: Handle special chars: @#$%^&*()_+-={}[]|\/:;""'<>?,.
// FIXME: Test unicode: 🚀 αβγ 汉字 العربية
// HACK: URL: https://example.com/path?param=value
";
        var filePath = Path.Combine(_tempDir, "special.cs");
        File.WriteAllText(filePath, content);
        var parser = new SourceTodoParser();

        // Act
        var result = parser.ExtractTodos(filePath).ToList();

        // Assert
        Assert.Equal(3, result.Count);

        var todo = result.FirstOrDefault(r => r.TodoKeyword == "TODO");
        Assert.NotNull(todo);
        Assert.Contains("@#$%^&*()", todo!.TodoText);

        var fixme = result.FirstOrDefault(r => r.TodoKeyword == "FIXME");
        Assert.NotNull(fixme);
        Assert.Contains("🚀", fixme!.TodoText);

        var hack = result.FirstOrDefault(r => r.TodoKeyword == "HACK");
        Assert.NotNull(hack);
        Assert.Contains("https://", hack!.TodoText);
    }
}