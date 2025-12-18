using Orgi.Core.Model;
using Orgi.Core.Parsing;
using Xunit;

namespace Orgi.Core.Tests;

public class ParserIntegrationTests
{
    private readonly string _testDataDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

    [Fact]
    public void Parse_RealWorldOrgFile_ParsesCorrectly()
    {
        // Arrange
        var content = @"# Project TODO list

* TODO [#A] Implement user authentication :backend:security:
  :PROPERTIES:
  :ID: auth-001
  :TITLE: Implement user authentication
  :DESCRIPTION: Add JWT-based authentication with refresh tokens
  :CREATED: <2025-12-18 Thu 09:00>
  :ASSIGNEE: alice@example.com
  :ESTIMATED_HOURS: 16
  :PRIORITY: high
  :END:

  - Set up JWT middleware
  - Implement login/logout endpoints
  - Add password reset functionality
  - Create user registration flow

* INPROGRESS [#B] Design database schema :database:
  :PROPERTIES:
  :ID: db-001
  :TITLE: Design database schema
  :DESCRIPTION: Create ERD and migration scripts for user and project tables
  :CREATED: <2025-12-17 Wed 14:30>
  :ASSIGNEE: bob@example.com
  :ESTIMATED_HOURS: 8
  :PRIORITY: medium
  :END:

  Working on the user schema currently.
  Need to add relationship tables tomorrow.

* DONE [#C] Setup development environment :setup:
  :PROPERTIES:
  :ID: setup-001
  :TITLE: Setup development environment
  :DESCRIPTION: Configure Docker, databases, and CI/CD pipeline
  :CREATED: <2025-12-15 Mon 10:00>
  :COMPLETED: <2025-12-16 Tue 17:30>
  :ASSIGNEE: charlie@example.com
  :ESTIMATED_HOURS: 12
  :PRIORITY: low
  :END:

  All services are running in Docker containers.
  GitHub Actions is configured for automated testing.

* KILL [#A] Implement legacy XML API :deprecated:
  :PROPERTIES:
  :ID: xml-api-001
  :TITLE: Implement legacy XML API
  :DESCRIPTION: Create XML endpoints for legacy system integration
  :CREATED: <2025-12-10 Tue 11:00>
  :CANCELLED: <2025-12-14 Sat 16:00>
  :ASSIGNEE: diana@example.com
  :ESTIMATED_HOURS: 20
  :PRIORITY: low
  :REASON: Client decided to use REST API instead
  :END:

  Cancelled because the legacy system was decommissioned.
  The team will focus on REST API development instead.

** TODO Bug: Parser crashes on empty files :bug:urgent:
   :PROPERTIES:
   :ID: bug-001
   :TITLE: Parser crashes on empty files
   :DESCRIPTION: The parser throws an unhandled exception when processing empty .org files
   :CREATED: <2025-12-18 Thu 08:15>
   :ASSIGNEE: alice@example.com
   :ESTIMATED_HOURS: 2
   :PRIORITY: urgent
   :STACK_TRACE: NullPointerException at Parser.parseLine:42
   :END:

   Need to add null checks at Parser.cs:42 and handle empty file gracefully.

*** TODO Research: Evaluate alternative parsers :research:
    :PROPERTIES:
    :ID: research-001
    :TITLE: Evaluate alternative parsing libraries
    :DESCRIPTION: Research and benchmark different org-mode parser implementations
    :CREATED: <2025-12-18 Thu 07:30>
    :ASSIGNEE: eve@example.com
    :ESTIMATED_HOURS: 4
    :PRIORITY: low
    :OPTIONS:
    - ANTLR-based parser
    - Hand-crafted recursive descent
    - Regex-based approach
    :END:

    Create a comparison matrix with performance benchmarks.";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Equal(6, issues.Count);

            // Verify first issue (high priority TODO)
            var authIssue = issues[0];
            Assert.Equal("auth-001", authIssue.Id);
            Assert.Equal("Implement user authentication", authIssue.Title);
            Assert.Equal(IssueState.Todo, authIssue.State);
            Assert.Equal(Priority.A, authIssue.Priority);
            Assert.Contains("backend", authIssue.Tags);
            Assert.Contains("security", authIssue.Tags);

            // Verify second issue (in progress)
            var dbIssue = issues[1];
            Assert.Equal("db-001", dbIssue.Id);
            Assert.Equal(IssueState.InProgress, dbIssue.State);
            Assert.Equal(Priority.B, dbIssue.Priority);
            Assert.Contains("database", dbIssue.Tags);

            // Verify third issue (completed)
            var setupIssue = issues[2];
            Assert.Equal("setup-001", setupIssue.Id);
            Assert.Equal(IssueState.Done, setupIssue.State);
            Assert.Equal(Priority.C, setupIssue.Priority);
            Assert.Contains("setup", setupIssue.Tags);

            // Verify fourth issue (killed)
            var xmlIssue = issues[3];
            Assert.Equal("xml-api-001", xmlIssue.Id);
            Assert.Equal(IssueState.Kill, xmlIssue.State);
            Assert.Equal(Priority.A, xmlIssue.Priority);
            Assert.Contains("deprecated", xmlIssue.Tags);

            // Verify fifth issue (sub-level bug)
            var bugIssue = issues[4];
            Assert.Equal("bug-001", bugIssue.Id);
            Assert.Equal("Parser crashes on empty files", bugIssue.Title);
            Assert.Equal(IssueState.Todo, bugIssue.State);
            Assert.Contains("bug", bugIssue.Tags);
            Assert.Contains("urgent", bugIssue.Tags);

            // Verify sixth issue (deep sub-level research)
            var researchIssue = issues[5];
            Assert.Equal("research-001", researchIssue.Id);
            Assert.Equal(IssueState.Todo, researchIssue.State);
            Assert.Contains("research", researchIssue.Tags);

            // Verify body content is preserved
            Assert.Contains("- Set up JWT middleware", authIssue.Description);
            Assert.Contains("Working on the user schema currently", dbIssue.Description);
            Assert.Contains("All services are running in Docker", setupIssue.Description);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_ComplexWorkflowFile_ParsesCorrectly()
    {
        // Arrange
        var content = @"# Sprint Backlog - Sprint 23

* TODO [#A] Critical: Fix production database connection :critical:production:
  :PROPERTIES:
  :ID: prod-db-fix-001
  :TITLE: Critical: Fix production database connection
  :DESCRIPTION: Production servers cannot connect to the primary database cluster
  :CREATED: <2025-12-18 Thu 02:00>
  :ASSIGNEE: oncall@example.com
  :ESTIMATED_HOURS: 4
  :IMPACT: High - All services affected
  :ROLLBACK: Manual intervention required
  :END:

  Immediate action required:
  1. Check database cluster status
  2. Verify network connectivity
  3. Failover to backup cluster if needed

* INPROGRESS [#B] Implement rate limiting :api:security:
  :PROPERTIES:
  :ID: rate-limit-001
  :TITLE: Implement rate limiting
  :DESCRIPTION: Add rate limiting to prevent API abuse and ensure fair usage
  :CREATED: <2025-12-17 Wed 10:30>
  :ASSIGNEE: backend-team@example.com
  :ESTIMATED_HOURS: 12
  :END:

  Implementation status:
  ✅ Redis integration complete
  ✅ Rate limiting algorithms implemented
  🔄 API middleware in progress
  ❌ Documentation pending

* TODO [#C] Update API documentation :documentation:
  :PROPERTIES:
  :ID: api-docs-update-001
  :TITLE: Update API documentation
  :DESCRIPTION: Update OpenAPI spec with new endpoints and examples
  :CREATED: <2025-12-16 Tue 14:00>
  :ASSIGNEE: tech-writer@example.com
  :ESTIMATED_HOURS: 6
  :PREREQUISITES: rate-limit-001
  :END:

  Documentation tasks:
  - Add new endpoint descriptions
  - Include rate limiting information
  - Update authentication examples
  - Add troubleshooting section

** TODO Security audit :security:audit:
   :PROPERTIES:
   :ID: security-audit-001
   :TITLE: Q4 Security Audit
   :DESCRIPTION: Perform comprehensive security audit of the application
   :CREATED: <2025-12-15 Mon 09:00>
   :ASSIGNEE: security-team@example.com
   :ESTIMATED_HOURS: 40
   :DEADLINE: <2025-12-31 Wed>
   :END:

   Audit checklist:
   - [ ] Review authentication mechanisms
   - [ ] Check for SQL injection vulnerabilities
   - [ ] Verify input validation
   - [ ] Audit dependency security
   - [ ] Review access controls
   - [ ] Test for XSS vulnerabilities
   - [ ] Check CSRF protection
   - [ ] Review logging and monitoring

*** TODO Performance optimization :performance:
    :PROPERTIES:
    :ID: perf-opt-001
    :TITLE: Optimize database queries
    :DESCRIPTION: Profile and optimize slow database queries identified in monitoring
    :CREATED: <2025-12-14 Sun 11:00>
    :ASSIGNEE: dba@example.com
    :ESTIMATED_HOURS: 16
    :METRICS:
    - Current avg response: 250ms
    - Target avg response: <100ms
    - Slow queries identified: 7
    :END:

    Query optimization plan:
    1. Add missing indexes
    2. Rewrite complex joins
    3. Implement query caching
    4. Add connection pooling optimization";

        var tempFile = CreateTempFile(content);
        var parser = new Parser(tempFile);

        try
        {
            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Equal(5, issues.Count);

            // Verify critical production issue
            var criticalIssue = issues[0];
            Assert.Equal("prod-db-fix-001", criticalIssue.Id);
            Assert.Equal(IssueState.Todo, criticalIssue.State);
            Assert.Equal(Priority.A, criticalIssue.Priority);
            Assert.Contains("critical", criticalIssue.Tags);
            Assert.Contains("production", criticalIssue.Tags);

            // Verify in-progress rate limiting
            var rateLimitIssue = issues[1];
            Assert.Equal("rate-limit-001", rateLimitIssue.Id);
            Assert.Equal(IssueState.InProgress, rateLimitIssue.State);

            // Verify documentation task with prerequisites
            var docsIssue = issues[2];
            Assert.Equal("api-docs-update-001", docsIssue.Id);
            Assert.Equal(IssueState.Todo, docsIssue.State);
            Assert.Equal(Priority.C, docsIssue.Priority);

            // Verify security audit sub-task
            var securityIssue = issues[3];
            Assert.Equal("security-audit-001", securityIssue.Id);
            Assert.Contains("audit", securityIssue.Tags);

            // Verify deep performance task
            var perfIssue = issues[4];
            Assert.Equal("perf-opt-001", perfIssue.Id);
            Assert.Contains("performance", perfIssue.Tags);

            // Verify multiline body content with checkboxes and lists
            Assert.Contains("Immediate action required:", criticalIssue.Description);
            Assert.Contains("✅ Redis integration complete", rateLimitIssue.Description);
            Assert.Contains("- [ ] Review authentication mechanisms", securityIssue.Description);
            Assert.Contains("1. Add missing indexes", perfIssue.Description);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parser_DefaultFilePath_UsesOrgiDirectory()
    {
        // Arrange
        var defaultPath = Path.Combine(Directory.GetCurrentDirectory(), ".orgi");
        Directory.CreateDirectory(defaultPath);
        var defaultFile = Path.Combine(defaultPath, "orgi.org");

        try
        {
            var content = @"* TODO Test issue
  :PROPERTIES:
  :ID: default-001
  :TITLE: Default Path Test
  :DESCRIPTION: Test with default file path
  :CREATED: <2025-12-18 Thu>
  :END:";

            File.WriteAllText(defaultFile, content);
            var parser = new Parser(); // Use default constructor

            // Act
            var issues = parser.Parse().ToList();

            // Assert
            Assert.Single(issues);
            Assert.Equal("default-001", issues.First().Id);
        }
        finally
        {
            if (File.Exists(defaultFile))
                File.Delete(defaultFile);
            if (Directory.Exists(defaultPath))
                Directory.Delete(defaultPath, true);
        }
    }

    [Fact]
    public async Task Parser_ConcurrentAccess_HandlesCorrectly()
    {
        // Arrange
        var content = @"* TODO Concurrent test issue
  :PROPERTIES:
  :ID: concurrent-001
  :TITLE: Concurrent Access Test
  :DESCRIPTION: Test thread safety of parser
  :CREATED: <2025-12-18 Thu>
  :END:

* TODO Another concurrent issue
  :PROPERTIES:
  :ID: concurrent-002
  :TITLE: Another Concurrent Test
  :DESCRIPTION: Second concurrent test
  :CREATED: <2025-12-18 Thu>
  :END:";

        var tempFile = CreateTempFile(content);

        try
        {
            var tasks = new List<Task<List<Issue>>>();

            // Act - Parse the same file concurrently
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var parser = new Parser(tempFile);
                    return parser.Parse().ToList();
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            foreach (var task in tasks)
            {
                var issues = task.Result;
                Assert.Equal(2, issues.Count);
                Assert.Contains(issues, i => i.Id == "concurrent-001");
                Assert.Contains(issues, i => i.Id == "concurrent-002");
            }
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