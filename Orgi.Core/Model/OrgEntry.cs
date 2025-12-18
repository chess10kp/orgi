namespace Orgi.Core.Model;

public record OrgEntry(
    string Headline,
    IssueState State,
    IReadOnlyDictionary<string, string> Properties,
    string Body
);