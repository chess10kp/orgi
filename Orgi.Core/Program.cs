using Orgi.Core.Parsing;
using System.Diagnostics;
using System.IO;

namespace Orgi.Core;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[0] == "init")
            {
                var dirPath = ".org";
                var initFilePath = Path.Combine(dirPath, "orgi.org");
                Directory.CreateDirectory(dirPath);
                File.WriteAllText(initFilePath, "");
                Console.WriteLine("Initialized orgi repository at .org/orgi.org");
                return;
            }

            if (args.Length > 0 && args[0] == "list")
            {
                var listFilePath = args.Length > 1 ? args[1] : ".org/orgi.org";
                Console.WriteLine(ListIssues(listFilePath));
                return;
            }

            if (args.Length > 0 && args[0] == "add")
            {
                var addArgs = args.Skip(1).ToArray();
                AddIssue(addArgs);
                return;
            }

            // Default behavior: list issues from default file or provided file
            var defaultFilePath = ".org/orgi.org";
            var parseFilePath = args.Length > 0 ? args[0] : defaultFilePath;
            Console.WriteLine(ListIssues(parseFilePath));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    public static string ListIssues(string filePath)
    {
        try
        {
            Parser parser = new(filePath);
            var issues = parser.Parse().ToList();
            var output = $"Found {issues.Count} issues:\n";
            foreach (var issue in issues)
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
            var filePath = ".org/orgi.org";
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
}

