using Orgi.Core.Parsing;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using Orgi.Core.Model;
using Orgi.Core.Sync;
using Orgi.Core.Discovery;

namespace Orgi.Core;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Orgi - Command-line tool for managing issues in Org mode files");

        // Positional argument for file
        var fileArgument = new Argument<FileInfo>(
            "file",
            description: "Path to the .orgi file",
            getDefaultValue: () => new FileInfo(".orgi/orgi.org"));
        rootCommand.AddArgument(fileArgument);

        // Global options
        var fileOption = new Option<FileInfo>(
            "--file",
            description: "Path to the .orgi file",
            getDefaultValue: () => new FileInfo(".orgi/orgi.org"));
        fileOption.AddAlias("-f");
        rootCommand.AddGlobalOption(fileOption);

        // Default handler for root command
        rootCommand.SetHandler((file) =>
        {
            try
            {
                Console.WriteLine(ListIssues(file.FullName, true));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, fileArgument);

        // Init command
        var initCommand = new Command("init", "Initialize a new orgi repository");
        initCommand.SetHandler(() =>
        {
            var dirPath = ".orgi";
            var initFilePath = Path.Combine(dirPath, "orgi.org");
            Directory.CreateDirectory(dirPath);
            File.WriteAllText(initFilePath, "");
            Console.WriteLine("Initialized orgi at .orgi/orgi.org");
        });
        rootCommand.AddCommand(initCommand);

        // List command
        var listCommand = new Command("list", "List issues");
        var allOption = new Option<bool>("--all", "List all issues (default: open only)");
        var openOption = new Option<bool>("--open", "List only open issues (TODO and INPROGRESS)");
        listCommand.AddOption(allOption);
        listCommand.AddOption(openOption);
        listCommand.SetHandler((all, open, file) =>
        {
            try
            {
                bool onlyOpen = !all && !open; // default to open if neither specified
                Console.WriteLine(ListIssues(file.FullName, onlyOpen));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, allOption, openOption, fileOption);
        rootCommand.AddCommand(listCommand);

        // Add command
        var addCommand = new Command("add", "Add a new issue");
        var titleArgument = new Argument<string[]>("title", "Title of the issue");
        var bodyOption = new Option<string?>("--body", "Body text for the issue");
        bodyOption.AddAlias("-b");
        addCommand.AddArgument(titleArgument);
        addCommand.AddOption(bodyOption);
        addCommand.SetHandler((title, body, file) =>
        {
            try
            {
                var addArgs = new List<string>();
                if (file != null && file.FullName != ".orgi/orgi.org")
                {
                    addArgs.Add(file.FullName);
                }
                if (body != null)
                {
                    addArgs.Add("-b");
                    addArgs.Add(body);
                }
                string joinedTitle = title != null && title.Length > 0 ? string.Join(" ", title) : null;
                AddIssue(joinedTitle, addArgs.ToArray());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, titleArgument, bodyOption, fileOption);
        rootCommand.AddCommand(addCommand);

        // Gather command
        var gatherCommand = new Command("gather", "Gather TODOs from source code files");
        var dryRunOption = new Option<bool>("--dry-run", "Show what would be gathered without making changes");
        gatherCommand.AddOption(dryRunOption);
        gatherCommand.SetHandler((dryRun) =>
        {
            try
            {
                var gatherArgs = dryRun ? new[] { "--dry-run" } : Array.Empty<string>();
                GatherCommand.Execute(gatherArgs);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, dryRunOption);
        rootCommand.AddCommand(gatherCommand);

        // Sync command
        var syncCommand = new Command("sync", "Sync completed issues back to source files");
        var autoConfirmOption = new Option<bool>("--auto-confirm", "Remove all completed TODOs without confirmation");
        syncCommand.AddOption(autoConfirmOption);
        syncCommand.SetHandler((autoConfirm) =>
        {
            try
            {
                var syncArgs = autoConfirm ? new[] { "--auto-confirm" } : Array.Empty<string>();
                SyncCommand.Execute(syncArgs);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, autoConfirmOption);
        rootCommand.AddCommand(syncCommand);

        // Done command
        var doneCommand = new Command("done", "Mark an issue as DONE");
        var idArgument = new Argument<string>("id", "Issue index or ID to mark as done");
        doneCommand.AddArgument(idArgument);
        doneCommand.SetHandler((id) =>
        {
            try
            {
                DoneCommand.Execute(new[] { id });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, idArgument);
        rootCommand.AddCommand(doneCommand);

        // Completion command
        var completionCommand = new Command("completion", "Generate shell completion scripts");
        var shellArgument = new Argument<string>("shell", "Shell type (bash or zsh)");
        completionCommand.AddArgument(shellArgument);
        completionCommand.SetHandler((shell) =>
        {
            if (shell == "bash")
            {
                // Output bash completion script
                Console.WriteLine(@"
# Bash completion for orgi
_orgi() {
    local cur prev words cword
    _get_comp_words_by_ref -n : cur prev words cword

    case $cword in
        1)
            COMPREPLY=( $(compgen -W 'init list add gather sync done --help --version' -- ""$cur"") )
            ;;
        2)
            case ${words[1]} in
                list)
                    COMPREPLY=( $(compgen -W '--all --open' -- ""$cur"") )
                    ;;
                 add)
                     COMPREPLY=( $(compgen -W '--body' -- ""$cur"") )
                     ;;
                gather)
                    COMPREPLY=( $(compgen -W '--dry-run' -- ""$cur"") )
                    ;;
                sync)
                    COMPREPLY=( $(compgen -W '--auto-confirm' -- ""$cur"") )
                    ;;
                done)
                    # Free input for index or ID
                    ;;
                *)
                    COMPREPLY=( $(compgen -f -X '!*.org' -- ""$cur"") )  # File completion for positional file
                    ;;
            esac
            ;;
        3)
            case ${words[1]} in
                add)
                    if [[ ${words[2]} == '--body' || ${words[2]} == '-b' ]]; then
                        # Body text, no completion
                        :
                    fi
                    ;;
                *)
                    COMPREPLY=( $(compgen -f -X '!*.org' -- ""$cur"") )
                    ;;
            esac
            ;;
        *)
            COMPREPLY=( $(compgen -f -X '!*.org' -- ""$cur"") )
            ;;
    esac
}

complete -F _orgi orgi
".Trim());
            }
            else if (shell == "zsh")
            {
                // Output zsh completion script
                Console.WriteLine(@"
# Zsh completion for orgi
_orgi() {
    _arguments -C \
        '1: :->command' \
        '*:: :->args'

    case $state in
        command)
            _values 'orgi command' \
                'init[initialize orgi repository]' \
                'list[list issues]' \
                'add[add new issue]' \
                'gather[gather TODOs from source]' \
                'sync[sync issues back to source]' \
                'done[mark issue as done]' \
                '--help[show help]' \
                '--version[show version]'
            ;;
        args)
            case $line[1] in
                list)
                    _arguments \
                        '--all[list all issues]' \
                        '--open[list open issues]' \
                        '1:: :_files -g ""*.org""'
                    ;;
                 add)
                     _arguments \
                         '1:title of the issue:title:' \
                         '(--body -b)'{--body,-b}'[body text]:body:' \
                         '2:: :_files -g ""*.org""'
                    ;;
                gather)
                    _arguments \
                        '--dry-run[show what would be gathered]'
                    ;;
                sync)
                    _arguments \
                        '--auto-confirm[remove without confirmation]'
                    ;;
                done)
                    _message 'index or issue ID'
                    ;;
                *)
                    _files -g '*.org'
                    ;;
            esac
            ;;
    esac
}
".Trim());
            }
            else
            {
                Console.Error.WriteLine("Unsupported shell. Supported: bash, zsh");
                Environment.Exit(1);
            }
        }, shellArgument);
        rootCommand.AddCommand(completionCommand);



        return await rootCommand.InvokeAsync(args);
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
            int index = 1;
            foreach (var issue in issuesToList)
            {
                output += $"  {index}. {issue.Id}: {issue.Title} ({issue.State}) [{issue.Priority}]\n";
                index++;
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

    public static void AddIssue(string? title, string[] args)
    {
        try
        {
            var filePath = ".orgi/orgi.org";
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

            string effectiveTitle;
            if (!string.IsNullOrEmpty(title))
            {
                effectiveTitle = title;
            }
            else
            {
                Console.Write("Title: ");
                effectiveTitle = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(effectiveTitle))
                {
                    Console.WriteLine("Title is required.");
                    return;
                }
            }

            string priority, state, tags;

            // Only prompt for additional information if title was not provided as argument
            if (!string.IsNullOrEmpty(title))
            {
                // Use default values when title is provided as argument
                priority = "";  // No priority by default
                state = "TODO"; // Default state is TODO
                tags = "";      // No tags by default
            }
            else
            {
                // Interactive mode - prompt for all values
                Console.Write("Priority (A/B/C/None): ");
                var priorityInput = Console.ReadLine()?.Trim().ToUpper();
                priority = priorityInput switch
                {
                    "A" => "A",
                    "B" => "B",
                    "C" => "C",
                    _ => ""
                };

                Console.Write("State (TODO/INPROGRESS/DONE/KILL): ");
                var stateInput = Console.ReadLine()?.Trim().ToUpper();
                state = stateInput switch
                {
                    "INPROGRESS" => "INPROGRESS",
                    "DONE" => "DONE",
                    "KILL" => "KILL",
                    _ => "TODO"
                };

                Console.Write("Tags (comma-separated): ");
                var tagsInput = Console.ReadLine()?.Trim();
                tags = string.IsNullOrEmpty(tagsInput) ? "" : " :" + string.Join(":", tagsInput.Split(',').Select(t => t.Trim())) + ":";
            }

            if (body == null)
            {
                if (!string.IsNullOrEmpty(title))
                {
                    body = "";
                }
                else
                {
                    body = GetBodyFromEditor();
                }
            }

            AddIssueToFile(filePath, effectiveTitle, priority, state, tags, body ?? "");
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
                Console.Error.WriteLine("Error: 'done' command requires exactly one argument: the issue index or ID");
                Console.Error.WriteLine("Usage: orgi done <index|issue_id>");
                Environment.Exit(1);
            }

            var arg = args[0];
            var filePath = ".orgi/orgi.org";

            try
            {
                Parser parser = new(filePath);
                var allIssues = parser.Parse().ToList();
                var openIssues = allIssues.Where(i => i.State == IssueState.Todo || i.State == IssueState.InProgress).ToList();
                Issue? issue = null;
                string identifier;

                if (int.TryParse(arg, out int index))
                {
                    // Treat as 1-based index into open issues
                    if (index < 1 || index > openIssues.Count)
                    {
                        Console.Error.WriteLine($"Error: Index '{index}' is out of range. Valid range: 1-{openIssues.Count}");
                        Environment.Exit(1);
                    }
                    issue = openIssues[index - 1];
                    identifier = $"{index} ({issue.Id})";
                }
                else
                {
                    // Treat as ID
                    issue = allIssues.FirstOrDefault(i => i.Id == arg);
                    if (issue == null)
                    {
                        Console.Error.WriteLine($"Error: Issue with ID '{arg}' not found.");
                        Environment.Exit(1);
                    }
                    identifier = arg;
                }

                issue.State = IssueState.Done;

                var content = string.Join("", allIssues.Select(IssueToContent));
                File.WriteAllText(filePath, content.TrimStart());
                Console.WriteLine($"Marked issue {identifier} as DONE");
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
            var orgFile = ".orgi/orgi.org";

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
            var orgFile = ".orgi/orgi.org";

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
