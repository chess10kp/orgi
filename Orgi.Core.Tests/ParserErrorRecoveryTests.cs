using Orgi.Core.Parsing;
using Xunit;

namespace Orgi.Core.Tests;

public class ParserErrorRecoveryTests
{
    [Fact]
    public void Parse_FileWithUnterminatedPropertiesDrawer_RecoversGracefully()
    {
        // Arrange
        var content = @"* TODO Issue with unterminated properties
  :PROPERTIES:
  :ID: test-001
  :TITLE: Test issue
  :CREATED: <2025-12-18 Thu>

  Body content.";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => parser.Parse().ToList());
            Assert.Contains("Parsing completed with", ex.Message);
            Assert.Contains("Unterminated properties drawer", ex.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_FileWithMalformedProperties_HandlesColonsInValues()
    {
        // Arrange
        var content = @"* TODO Issue with colon in property value
  :PROPERTIES:
  :ID: colon-test-001
  :TITLE: Issue with colon
  :DESCRIPTION: URL: https://example.com
  :CREATED: <2025-12-18 Thu>
  :END:

  Body content.";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            Assert.Equal("colon-test-001", issues.First().Id);
            Assert.Equal("Issue with colon", issues.First().Title);
            Assert.Equal("URL: https://example.com", issues.First().Description);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_FileWithMixedValidAndInvalidIssues_ParsesValidOnes()
    {
        // Arrange
        var content = @"* TODO Valid issue 1
  :PROPERTIES:
  :ID: valid-001
  :TITLE: Valid issue 1
  :CREATED: <2025-12-18 Thu>
  :END:

  Valid issue 1 content.

* INVALID Invalid issue state
  :PROPERTIES:
  :ID: invalid-001
  :TITLE: Invalid issue
  :CREATED: <2025-12-18 Thu>
  :END:

  Invalid issue content.

* TODO Valid issue 2
  :PROPERTIES:
  :ID: valid-002
  :TITLE: Valid issue 2
  :CREATED: <2025-12-18 Thu>
  :END:

  Valid issue 2 content.";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => parser.Parse().ToList());
            Assert.Contains("Parsing completed with", ex.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private string CreateTempFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        return tempFile;
    }
}