using Orgi.Core.Parsing;
using System.Diagnostics;
using System.IO;
using Orgi.Core.Model;
using Orgi.Core.Sync;
using Orgi.Core.Discovery;

namespace Orgi.Core;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
            {
                ShowHelp();
                return;
            }

            if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
            {
                ShowVersion();
                return;
            }

            if (args.Length > 0 && args[0] == "init")
            {
                var dirPath = ".orgi";
                var initFilePath = Path.Combine(dirPath, "orgi.orgi");
                Directory.CreateDirectory(dirPath);
                File.WriteAllText(initFilePath, "");
                Console.WriteLine("Initialized orgi at .orgi/orgi.orgi");
                return;
            }

            if (args.Length > 0 && args[0] == "list")
            {
                bool onlyOpen = true;
                string listFilePath = ".orgi/orgi.orgi";
                if (args.Length > 1)
                {
                    if (args[1] == "all")
                    {
                        onlyOpen = false;
                        listFilePath = args.Length > 2 ? args[2] : ".orgi/orgi.orgi";
                    }
                    else if (args[1] == "open")
                    {
                        onlyOpen = true;
                        listFilePath = args.Length > 2 ? args[2] : ".orgi/orgi.orgi";
                    }
                    else
                    {
                        listFilePath = args[1];
    }
}

                Console.WriteLine(ListIssues(listFilePath, onlyOpen));
                return;
            }

            if (args.Length > 0 && args[0] == "add")
            {
                var addArgs = args.Skip(1).ToArray();
                AddIssue(addArgs);
                return;
            }

            if (args.Length > 0 && args[0] == "gather")
            {
                var gatherArgs = args.Skip(1).ToArray();
                GatherCommand.Execute(gatherArgs);
                return;
            }

            if (args.Length > 0 && args[0] == "sync")
            {
                var syncArgs = args.Skip(1).ToArray();
                SyncCommand.Execute(syncArgs);
                return;
            }

            if (args.Length > 0 && args[0] == "done")
            {
                var doneArgs = args.Skip(1).ToArray();
                DoneCommand.Execute(doneArgs);
                return;
            }

            // Handle unknown command
            if (args.Length > 0 && !File.Exists(args[0]) && args[0] != "list" && args[0] != "add" && args[0] != "gather" && args[0] != "sync" && args[0] != "done")
            {
                Console.Error.WriteLine($"Error: Unknown command '{args[0]}'");
                Console.Error.WriteLine("Use 'orgi --help' for usage information");
                Environment.Exit(1);
            }

            // Default behavior: list issues from default file or provided file
            var  defaultFilePath = ".orgi/orgi.org";
            var parseFilePath = args.Length > 0 ? args[0] : defaultFilePath;
            var listAll = args.Length > 1 && args[1] == "all";
            Console.WriteLine(ListIssues(parseFilePath, !listAll)); // list "all" for all and "open" for just open
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    public static string ListIssues(string filePath, bool onlyOpenTodos = true)
    {
        try
        {
            Parser parser = new(filePath);
            var allIssues = parser.Parse().ToList();
            var issuesToList = onlyOpenTodos ? allIssues.Where(i => i.State == Model.IssueState.Todo || i.State == Model.IssueState.InProgress).ToList() : allIssues;
            if (issuesToList.Count == 0)
            {
                return onlyOpenTodos ? "no open issues" : "no issues";
            }
            var issueType = onlyOpenTodos ? "open issues" : "issues";
            var output = $"Found {issuesToList.Count} {issueType}:\n";
            foreach (var issue in issuesToList)
            {
                output += $"  {issue.Id}: {issue.Title} ({issue.State}) [{issue.Priority}]\n";
            }
            return output.TrimEnd();
        }
        catch (FileNotFoundException)
        {
            return $"File not found: {filePath}. Maybe first run orgi init?";
        }
        catch (FormatException ex)
        {
            return $"Error: Invalid file format in {filePath}: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error: Failed to parse file {filePath}: {ex.Message}";
        }
    }

    public static void AddIssue(string[] args)
    {
        try
        {
            var filePath = ".orgi/orgi.orgi";
            string? body = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-b" && i + 1 < args.Length)
                {
                    body = args[i + 1];
                    i++; // skip next
                }
                else if (!args[i].StartsWith("-"))
                {
                    filePath = args[i];
    }
}


            Console.Write("Title: ");
            var title = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(title))
            {
                Console.WriteLine("Title is required.");
                return;
            }

            Console.Write("Priority (A/B/C/None): ");
            var priorityInput = Console.ReadLine()?.Trim().ToUpper();
            var priority = priorityInput switch
            {
                "A" => "A",
                "B" => "B",
                "C" => "C",
                _ => ""
            };

            Console.Write("State (TODO/INPROGRESS/DONE/KILL): ");
            var stateInput = Console.ReadLine()?.Trim().ToUpper();
            var state = stateInput switch
            {
                "INPROGRESS" => "INPROGRESS",
                "DONE" => "DONE",
                "KILL" => "KILL",
                _ => "TODO"
            };

            Console.Write("Tags (comma-separated): ");
            var tagsInput = Console.ReadLine()?.Trim();
            var tags = string.IsNullOrEmpty(tagsInput) ? "" : " :" + string.Join(":", tagsInput.Split(',').Select(t => t.Trim())) + ":";

            if (body == null)
            {
                body = GetBodyFromEditor();
            }

            AddIssueToFile(filePath, title, priority, state, tags, body ?? "");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error adding issue: {ex.Message}");
        }
    }

    private static string? GetBodyFromEditor()
    {
        try
        {
            var editor = Environment.GetEnvironmentVariable("EDITOR") ?? "nano";
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "# Enter the body of the issue below\n# Lines starting with # will be ignored\n\n");

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = editor,
                    Arguments = tempFile,
                    UseShellExecute = true
                });

                if (process == null)
                {
                    throw new InvalidOperationException($"Failed to start editor: {editor}");
                }

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Editor exited with code {process.ExitCode}");
                }

                var content = File.ReadAllText(tempFile);
                var lines = content.Split('\n');
                var bodyLines = lines.Where(line => !line.TrimStart().StartsWith("#")).ToArray();
                return string.Join("\n", bodyLines).Trim();
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error using editor: {ex.Message}");
            Console.WriteLine("Please enter the body manually:");
            return Console.ReadLine()?.Trim() ?? "";
        }
    }

    public static void AddIssueToFile(string filePath, string title, string priority, string state, string tags, string body)
    {
        try
        {
            var id = "task-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var created = DateTime.Now.ToString("<yyyy-MM-dd ddd HH:mm>");

            var priorityStr = string.IsNullOrEmpty(priority) ? "" : $" [#{priority}]";

            var content = $"\n* {state}{priorityStr} {title}{tags}\n  :PROPERTIES:\n  :ID: {id}\n  :TITLE: {title}\n  :CREATED: {created}\n  :END:\n\n  {body}\n";

            File.AppendAllText(filePath, content);
            Console.WriteLine($"Added issue {id}");
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Error: Permission denied writing to file: {filePath}");
        }
        catch (DirectoryNotFoundException)
        {
            Console.Error.WriteLine($"Error: Directory not found for file: {filePath}");
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Error: Failed to write to file {filePath}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: Unexpected error writing to file {filePath}: {ex.Message}");
        }
    }

    private static string IssueToContent(Issue issue)
    {
        var priorityStr = issue.Priority == Priority.None ? "" : $" [#{issue.Priority}]";
        var tagsStr = issue.Tags.Any() ? " :" + string.Join(":", issue.Tags) + ":" : "";
        var stateStr = issue.State.ToString().ToUpper();
        var created = issue.CreatedAt.ToString("<yyyy-MM-dd ddd HH:mm>");
        var body = issue.Description.Trim();

        var propertiesLines = new List<string>();
        foreach (var prop in issue.Properties)
        {
            if (prop.Key != "tags" && prop.Key != "priority") // handled in headline
            {
                propertiesLines.Add($"  :{prop.Key}: {prop.Value}");
            }
        }
        var propertiesBlock = string.Join("\n", propertiesLines);

        var content = $"\n* {stateStr}{priorityStr} {issue.Title}{tagsStr}\n  :PROPERTIES:\n{propertiesBlock}\n  :END:\n\n  {body}\n";
        return content;
    }

    /// <summary>
    /// Handles the done command
    /// </summary>
    public static class DoneCommand
    {
        public static void Execute(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Error: 'done' command requires exactly one argument: the issue ID");
                Console.Error.WriteLine("Usage: orgi done <issue_id>");
                Environment.Exit(1);
            }

            var issueId = args[0];
            var filePath = ".orgi/orgi.orgi";

            try
            {
                Parser parser = new(filePath);
                var issues = parser.Parse().ToList();
                var issue = issues.FirstOrDefault(i => i.Id == issueId);
                if (issue == null)
                {
                    Console.Error.WriteLine($"Error: Issue with ID '{issueId}' not found.");
                    Environment.Exit(1);
                }

                issue.State = IssueState.Done;

                var content = string.Join("", issues.Select(IssueToContent));
                File.WriteAllText(filePath, content.TrimStart());
                Console.WriteLine($"Marked issue {issueId} as DONE");
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine($"Error: Orgi repository not initialized. Run 'orgi init' first.");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error during done: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }

    private static void ShowHelp()
    {
        var helpText = @"
Orgi - Command-line tool for managing issues in Org mode files

USAGE:
    orgi [COMMAND] [OPTIONS]

 COMMANDS:
     init                    Initialize a new orgi repository
     list [all|open] [file]  List issues
     add [file] [OPTIONS]    Add a new issue
     gather [OPTIONS]        Gather TODOs from source code files
     sync [OPTIONS]          Sync orgi issues with source code
     done <issue_id>         Mark an issue as DONE
     --help, -h              Show this help message
     --version, -v           Show version information

LIST COMMAND:
    orgi list               List open issues from .orgi/orgi.orgi
    orgi list all           List all issues from .orgi/orgi.orgi
    orgi list <file>        List open issues from specified file
    orgi list all <file>    List all issues from specified file

ADD COMMAND:
    orgi add               Add new issue interactively
    orgi add <file>        Add new issue to specified file

GATHER COMMAND:
    orgi gather                         Gather TODOs from current directory
    orgi gather --dry-run              Show what would be gathered without making changes

 SYNC COMMAND:
     orgi sync                          Sync completed issues back to source files
     orgi sync --auto-confirm           Remove all completed TODOs without confirmation

 DONE COMMAND:
     orgi done <issue_id>               Mark an issue as DONE
";
        Console.WriteLine(helpText.Trim());
    }

    private static void ShowVersion()
    {
        Console.WriteLine("Orgi version 0.1.2");
        Console.WriteLine("A command-line tool for managing issues in Org mode files");
    }
}

/// <summary>
/// Handles the gather command
/// </summary>
public static class GatherCommand
{
    public static void Execute(string[] args)
    {
        try
        {
            var dryRun = args.Contains("--dry-run");
            var sourceDir = Directory.GetCurrentDirectory();
            var orgFile = ".orgi/orgi.orgi";

            // Ensure orgi repository is initialized
            if (!File.Exists(orgFile))
            {
                Console.Error.WriteLine("Error: Orgi repository not initialized. Run 'orgi init' first.");
                Environment.Exit(1);
            }

            Console.WriteLine("Scanning source files for TODOs...");
            var synchronizer = new IssueSynchronizer();
            var result = synchronizer.GatherFromSource(sourceDir, orgFile, dryRun);

            Console.WriteLine($"\nGather Results:");
            Console.WriteLine($"  Source files scanned: {result.SourceFilesScanned}");
            Console.WriteLine($"  TODOs found: {result.TodosFound}");
            Console.WriteLine($"  Existing orgi issues: {result.ExistingIssuesFound}");
            Console.WriteLine($"  Already matched: {result.MatchedIssues}");
            Console.WriteLine($"  New issues created: {result.NewIssuesCreated}");
            Console.WriteLine($"  Source files modified: {result.FilesModified}");

            if (dryRun)
            {
                Console.WriteLine("\nThis was a dry run. No files were modified.");
                Console.WriteLine("Run without --dry-run to actually gather TODOs.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during gather: {ex.Message}");
            Environment.Exit(1);
        }
    }
}

/// <summary>
/// Handles the sync command
/// </summary>
public static class SyncCommand
{
    public static void Execute(string[] args)
    {
        try
        {
            var autoConfirm = args.Contains("--auto-confirm");
            var orgFile = ".orgi/orgi.orgi";

            // Ensure orgi repository is initialized
            if (!File.Exists(orgFile))
            {
                Console.Error.WriteLine("Error: Orgi repository not initialized. Run 'orgi init' first.");
                Environment.Exit(1);
            }

            Console.WriteLine("Syncing completed orgi issues back to source files...");
            var synchronizer = new IssueSynchronizer();
            var result = synchronizer.SyncToSource(orgFile, autoConfirm);

            Console.WriteLine($"\nSync Results:");
            Console.WriteLine($"  Total issues checked: {result.TotalIssuesChecked}");
            Console.WriteLine($"  Completed issues with source references: {result.CompletedSourceIssuesFound}");
            Console.WriteLine($"  TODOs removed from source: {result.TodosRemoved}");
            Console.WriteLine($"  TODOs skipped: {result.TodosSkipped}");
            Console.WriteLine($"  Source files modified: {result.FilesModified}");

            if (result.TodosRemoved > 0)
            {
                Console.WriteLine("\nCompleted TODOs have been removed from source files.");
            }
            else if (result.CompletedSourceIssuesFound > 0)
            {
                Console.WriteLine("\nNo TODOs were removed. All completed issues were skipped.");
            }
            else
            {
                Console.WriteLine("\nNo completed issues with source references found.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during sync: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
