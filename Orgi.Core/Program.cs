using Orgi.Core.Parsing;
using System.IO;

namespace Orgi.Core;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "init")
        {
            var dirPath = ".org";
            var filePath = Path.Combine(dirPath, "orgi.org");
            Directory.CreateDirectory(dirPath);
            File.WriteAllText(filePath, "");
            Console.WriteLine("Initialized orgi repository at .org/orgi.org");
            return;
        }

        var defaultFilePath = ".org/orgi.org";
        var filePathToParse = args.Length > 0 ? args[0] : defaultFilePath;
        Parser parser = new(filePathToParse);
        var issues = parser.Parse().ToList();
        Console.WriteLine($"Found {issues.Count} issues:");
        foreach (var issue in issues)
        {
            Console.WriteLine($"  {issue.Id}: {issue.Title} ({issue.State}) [{issue.Priority}]");
        }
    }
}

