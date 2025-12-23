using Orgi.Core.Model;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Orgi.Core.Parsing;

enum ParserState
{
    Keyword,
    Headline,
    Properties,
    Body,
    Unassigned
}

public class Parser(string filePath = ".orgi/orgi.org")
{
    private readonly string _filePath = filePath;
    private readonly List<Issue> _issues = [];
    private string _line = string.Empty;
    private string _headline = string.Empty;
    private int _ptr = 0;
    private int _currentLevel = 0;
    private IssueState _currentIssueState;
    private Priority _currentPriority = Priority.None;
    private ParserState _state = ParserState.Unassigned;

    private static readonly Dictionary<string, IssueState> IssueMap = new()
    {
        // TODO: allow for custom todo states
        { "TODO", IssueState.Todo },
        { "INPROGRESS", IssueState.InProgress },
        { "DONE", IssueState.Done },
        { "KILL", IssueState.Kill },
    };

    private void Consume() => _ptr++;
    private char Peek() => _ptr < _line.Length ? _line[_ptr] : '\0';

    private bool ConsumeTill(char c)
    {
        while (_ptr < _line.Length && Peek() != c)
        {
            Consume();
        }

        return _ptr < _line.Length;
    }

    private void ConsumeUntil(Func<char, bool> predicate)
    {
        while (_ptr < _line.Length && predicate(_line[_ptr]))
        {
            _ptr++;
        }
    }

    private string PeekTill(string c, bool consume = false)
    {
        int start = _ptr;
        int end = _line.IndexOf(c, _ptr, StringComparison.Ordinal);
        if (consume) _ptr = end;
        return end == -1 ? _line[start..] : _line[start..end];
    }

    private string PeekUntil(Func<char, bool> predicate, bool consume = false)
    {
        int start = _ptr;
        while (predicate(_line[_ptr]))
        {
            _ptr++;
        }
        if (consume) _ptr = start;

        return _line[start.._ptr];
    }

    // called after leading stars, need to parse issue state
    private void ParseIssueState()
    {
        if (_ptr >= _line.Length) throw new FormatException("Expected issue state but found end of line: " + _line);
        
        // Skip whitespace
        while (_ptr < _line.Length && char.IsWhiteSpace(_line[_ptr]))
        {
            _ptr++;
        }
        
        if (_ptr >= _line.Length) throw new FormatException("Expected issue state but found end of line: " + _line);
        
        // Find the end of the issue state keyword
        var start = _ptr;
        while (_ptr < _line.Length && !char.IsWhiteSpace(_line[_ptr]) && _line[_ptr] != '[')
        {
            _ptr++;
        }
        
        var issueStateStr = _line[start.._ptr];
        if (string.IsNullOrEmpty(issueStateStr))
        {
            throw new FormatException($"Empty issue state on line: {_line}");
        }
        
        if (!IssueMap.ContainsKey(issueStateStr))
        {
            throw new FormatException($"Unknown issue state: {issueStateStr} on line: {_line}");
        }
        _currentIssueState = IssueMap[issueStateStr];
    }

    private void ParseHeading()
    {
        
        if (_state != ParserState.Unassigned) throw new FormatException($"Expected header but found: {_state}");
        if (!_line.StartsWith("*")) throw new FormatException($"Expected * when parsing header, but found: {_line}");
        // Count stars for level
        _currentLevel = 0;
        while (_ptr < _line.Length && _line[_ptr] == '*')
        {
            _currentLevel++;
            _ptr++;
        }
        
        ConsumeUntil(c => char.IsWhiteSpace(c));

        // Parse issue state
        ParseIssueState();

        // Skip whitespace after issue state
        ConsumeUntil(c => char.IsWhiteSpace(c));

        // Check for priority [#A]
        _currentPriority = Priority.None;
        if (_ptr + 3 < _line.Length && _line[_ptr] == '[' && _line[_ptr + 1] == '#' && _line[_ptr + 3] == ']')
        {
            var priorityChar = char.ToUpper(_line[_ptr + 2]);
            if (priorityChar == 'A' || priorityChar == 'B' || priorityChar == 'C')
            {
                _currentPriority = priorityChar switch
                {
                    'A' => Priority.A,
                    'B' => Priority.B,
                    'C' => Priority.C,
                    // TODO: add more priority
                    _ => Priority.None
                };
                _ptr += 4; // Skip [#X]
            }
        }

        // Skip whitespace after priority
        ConsumeUntil(c => char.IsWhiteSpace(c));
        
        // Extract headline text (everything until tags or end of line)
        var headlineStart = _ptr;
        _headline = _line[headlineStart..].Trim();
        
        if (string.IsNullOrWhiteSpace(_headline))
        {
            throw new FormatException($"Headline cannot be empty: {_line}");
        }
    }



    // parses all properties, stores them in dict
    private void ParseProperties()
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Expect :PROPERTIES: line
        if (!_line.Trim().Equals(":PROPERTIES:", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException($"Expected :PROPERTIES: but found: {_line}");
        }
        
        _state = ParserState.Properties;
        
        // Read lines until :END:
        using var reader = new StreamReader(_filePath);
        
        // For now, let's assume we're reading line by line in the main Parse method
        // This method will be called when we encounter :PROPERTIES:
        // and we'll continue reading until :END:
    }

    // parses body
    private void ParseBody()
    {
        var bodyLines = new List<string>();
        _state = ParserState.Body;
        
        // Continue reading until next headline or end of file
        // This will be handled in the main Parse method
    }


    public IEnumerable<Issue> Parse()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException($"Orgi file not found: {_filePath}");
        }

        var currentProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var currentBody = new List<string>();
        var currentTags = new List<string>();

        using var reader = new StreamReader(_filePath);
        while (reader.ReadLine() is { } currentLine)
        {
            _line = currentLine;
            _ptr = 0;

            if (string.IsNullOrWhiteSpace(_line)) continue;

            // If we hit a new headline and we have a pending issue, finalize it
            if (_line.StartsWith('*') && _state != ParserState.Unassigned)
            {
                if (_state == ParserState.Properties)
                {
                    throw new FormatException("Unterminated properties drawer");
                }
                CreateCurrentIssue(currentProperties, currentBody, currentTags);
                currentProperties.Clear();
                currentBody.Clear();
                currentTags.Clear();
                _state = ParserState.Unassigned;
            }

            if (_line.StartsWith('*'))
            {
                ParseHeading();
                _state = ParserState.Headline;
            }
            else if (_state == ParserState.Headline || _state == ParserState.Properties)
            {
                if (_line.Trim().Equals(":PROPERTIES:", StringComparison.OrdinalIgnoreCase))
                {
                    _state = ParserState.Properties;
                    // Continue reading properties
                }
                else if (_line.Trim().Equals(":END:", StringComparison.OrdinalIgnoreCase))
                {
                    _state = ParserState.Body;
                }
                else if (_state == ParserState.Properties && _line.TrimStart().StartsWith(':') && _line.Contains(':'))
                {
                    ParsePropertyLine(currentProperties);
                }
                else
                {
                    // Not a property line, switch to body
                    _state = ParserState.Body;
                    currentBody.Add(_line);
                }
            }
            else if (_state == ParserState.Body)
            {
                currentBody.Add(_line);
            }
        }

        // Finalize the last issue if there is one
        if (_state != ParserState.Unassigned)
        {
            if (_state == ParserState.Properties)
            {
                throw new FormatException("Unterminated properties drawer");
            }
            CreateCurrentIssue(currentProperties, currentBody, currentTags);
        }

        return _issues;
    }

    private void ParsePropertyLine(Dictionary<string, string> properties)
    {
        var trimmed = _line.TrimStart();
        if (!trimmed.StartsWith(':') || !trimmed.Contains(':'))
        {
            throw new FormatException($"Invalid property line format: {_line}");
        }

        var firstColon = trimmed.IndexOf(':', 1);
        if (firstColon == -1)
        {
            throw new FormatException($"Invalid property line format: {_line}");
        }

        var key = trimmed[1..firstColon].Trim();
        var value = trimmed[(firstColon + 1)..].Trim();

        if (string.IsNullOrEmpty(key))
        {
            throw new FormatException($"Property key cannot be empty: {_line}");
        }

        properties[key] = value;
    }

    private void CreateCurrentIssue(Dictionary<string, string> properties, List<string> bodyLines, List<string> tags)
    {
        if (string.IsNullOrEmpty(_headline))
        {
            throw new InvalidOperationException("Cannot finalize issue without headline");
        }

        // Extract tags from headline if present
        ExtractTagsFromHeadline(tags);

        // Add tags to properties
        if (tags.Count > 0)
        {
            properties["tags"] = string.Join(":", tags);
        }

        // Add priority to properties if not None
        if (_currentPriority != Priority.None)
        {
            properties["priority"] = _currentPriority.ToString();
        }

        var orgEntry = new OrgEntry(
            _headline,
            _currentIssueState,
            new ReadOnlyDictionary<string, string>(properties),
            string.Join("\n", bodyLines)
        );

        try
        {
            var issue = Issue.FromOrgEntry(orgEntry);
            _issues.Add(issue);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create issue from entry '{_headline}': {ex.Message}", ex);
        }
    }

    private void ExtractTagsFromHeadline(List<string> tags)
    {
        var match = Regex.Match(_headline, @"(:[^:]+)+:$");
        if (match.Success)
        {
            var tagStr = match.Value.Trim(':');
            var extractedTags = tagStr.Split(':', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim());
            tags.AddRange(extractedTags);

            // Remove tags from headline
            _headline = _headline[..match.Index].Trim();
        }
    }
}
