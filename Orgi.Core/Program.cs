using Orgi.Core.Parsing;

namespace Orgi.Core;

public static class Program
{
    public static void Main(string[] args)
    {
        var filePath = args.Length > 0 ? args[0] : ".orgi/orgi.org";
        Parser parser = new(filePath);
        var issues = parser.Parse().ToList();
        Console.WriteLine($"Found {issues.Count} issues:");
        foreach (var issue in issues)
        {
            Console.WriteLine($"  {issue.Id}: {issue.Title} ({issue.State}) [{issue.Priority}]");
        }
    }
}

