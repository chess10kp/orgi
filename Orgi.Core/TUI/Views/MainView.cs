using Terminal.Gui;
using Orgi.Core.Parsing;
using Orgi.Core.Model;
using Orgi.Core;

namespace Orgi.Core.TUI;

public class MainView : Window
{
    private TabView _tabView = null!;
    private StatusBar _statusBar = null!;
    private IssuesTab _issuesTab = null!;
    private PRTab _prTab = null!;

    public MainView() : base("orgi")
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        _tabView = new TabView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        _issuesTab = new IssuesTab();
        _prTab = new PRTab();

        var issuesTab = new TabView.Tab("Issues", _issuesTab);
        var prTab = new TabView.Tab("Pull Requests", _prTab);
        _tabView.AddTab(issuesTab, true);
        _tabView.AddTab(prTab, false);

        Add(_tabView);

        _statusBar = new StatusBar(new StatusItem[]
        {
            new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => { Application.RequestStop(); }),
            new StatusItem(Key.Esc, "~Esc~ Quit", () => { Application.RequestStop(); }),
            new StatusItem(Key.R, "~R~ Refresh", Refresh),
            new StatusItem(Key.Tab, "~Tab~ Switch Tab", () => { _tabView.SelectedTab = _tabView.SelectedTab == issuesTab ? prTab : issuesTab; })
        });

        Add(_statusBar);

        Application.Resized += HandleResize;
    }

    private void HandleResize(Application.ResizedEventArgs args)
    {
        try
        {
            _issuesTab.Refresh();
            _prTab.Refresh();
        }
        catch (Exception)
        {
        }
    }

    private void Refresh()
    {
        try
        {
            _issuesTab.Refresh();
            _prTab.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Failed to refresh: {ex.Message}", "OK");
        }
    }

    public override bool ProcessKey(KeyEvent key)
    {
        var issuesTab = _tabView.Tabs.FirstOrDefault(t => t.View == _issuesTab);
        var prTab = _tabView.Tabs.FirstOrDefault(t => t.View == _prTab);
        var isIssuesTab = _tabView.SelectedTab == issuesTab;

        if (key.KeyValue == (uint)'j')
        {
            if (isIssuesTab)
            {
                _issuesTab.ScrollDown();
            }
            else
            {
                _prTab.ScrollDown();
            }
            return true;
        }
        if (key.KeyValue == (uint)'k')
        {
            if (isIssuesTab)
            {
                _issuesTab.ScrollUp();
            }
            else
            {
                _prTab.ScrollUp();
            }
            return true;
        }
        return base.ProcessKey(key);
    }
}

public abstract class BaseTab : View
{
    protected string OrgiFilePath = ".orgi/orgi.org";

    public BaseTab()
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    public abstract void Refresh();
}

public class IssuesTab : BaseTab
{
    private IssueListView _issueListView = null!;
    private List<string> _issueDisplayList = new();
    private List<Issue> _issues = new();
    private StatusBar? _tabStatusBar;

    private Action? _showFilterAction;
    private bool _terminalTooSmall = false;

    private class IssueListView : ListView
    {
        private List<Issue> _allIssues;
        private Action<string> _onFilter;
        private Action _showFilterAction;

        public IssueListView(List<string> source, List<Issue> allIssues, Action<string> onFilter, Action showFilterAction) : base(source)
        {
            _allIssues = allIssues;
            _onFilter = onFilter;
            _showFilterAction = showFilterAction;
        }

        public override bool ProcessKey(KeyEvent key)
        {
            if (key.KeyValue == (uint)'/')
            {
                _showFilterAction();
                return true;
            }
            return base.ProcessKey(key);
        }

        private void ShowFilterDialog()
        {
            var filterDialog = new FilterDialog("Filter Issues", _onFilter);
            Application.Run(filterDialog);
        }
    }

    public IssuesTab()
    {
        LoadIssues();
        _showFilterAction = ShowFilterDialog;
        CreateUI();
    }

    private void LoadIssues()
    {
        try
        {
            var parser = new Parser(OrgiFilePath);
            _issues = parser.Parse().ToList();
            UpdateDisplayList();
        }
        catch (FileNotFoundException)
        {
            MessageBox.ErrorQuery("Error", "Orgi repository not initialized. Run 'orgi init' first.", "OK");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Failed to load issues: {ex.Message}", "OK");
        }
    }

    private string? _currentFilter = null;

    private const int MinTerminalWidth = 90;
    private const int StateColumnWidth = 8;
    private const int IdColumnWidth = 18;

    private string RepeatChar(char c, int count)
    {
        return new string(c, count);
    }

    private string RepeatString(string s, int count)
    {
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++)
        {
            result.Append(s);
        }
        return result.ToString();
    }

    private string PadLeft(string s, int width)
    {
        return s.PadLeft(width);
    }

    private string PadRight(string s, int width)
    {
        return s.PadRight(width);
    }

    private (int stateWidth, int idWidth, int prioWidth, int titleWidth, int tagsWidth) CalculateColumnWidths()
    {
        var terminalWidth = Application.Top.Frame.Width;
        _terminalTooSmall = terminalWidth < MinTerminalWidth;

        if (_terminalTooSmall)
        {
            return (StateColumnWidth, IdColumnWidth, 5, 20, 8);
        }

        var fixedWidth = StateColumnWidth + IdColumnWidth + 3;
        var remainingWidth = terminalWidth - fixedWidth - 10;
        var equalColumnWidth = remainingWidth / 3;

        return (StateColumnWidth, IdColumnWidth, equalColumnWidth, equalColumnWidth, equalColumnWidth);
    }

    private List<Issue> GetFilteredIssues()
    {
        if (string.IsNullOrWhiteSpace(_currentFilter))
            return _issues;

        var filter = _currentFilter.ToLower();
        return _issues.Where(i =>
            i.Title.ToLower().Contains(filter) ||
            i.Id.ToLower().Contains(filter) ||
            i.Tags.Any(t => t.ToLower().Contains(filter)) ||
            i.State.ToString().ToLower().Contains(filter) ||
            i.Priority.ToString().ToLower().Contains(filter)
        ).ToList();
    }

    private void UpdateDisplayList()
    {
        _issueDisplayList.Clear();
        var issuesToShow = GetFilteredIssues();

        var (stateWidth, idWidth, prioWidth, titleWidth, tagsWidth) = CalculateColumnWidths();

        if (_terminalTooSmall)
        {
            _issueDisplayList.Add("Terminal too small. Please resize to at least 90 columns.");
            return;
        }

        var header = $"│ STATE{RepeatString(" ", stateWidth - 4)}│ ID{RepeatString(" ", idWidth - 1)}│ PRIO{RepeatString(" ", prioWidth - 3)}│ TITLE{RepeatString(" ", titleWidth - 4)}│ TAGS{RepeatString(" ", tagsWidth - 4)}│";
        var separator = $"├{RepeatChar('─', stateWidth + 2)}┼{RepeatChar('─', idWidth + 2)}┼{RepeatChar('─', prioWidth + 2)}┼{RepeatChar('─', titleWidth + 2)}┼{RepeatChar('─', tagsWidth + 2)}┤";
        _issueDisplayList.Add(header);
        _issueDisplayList.Add(separator);

        foreach (var issue in issuesToShow)
        {
            var stateIcon = GetStateIcon(issue.State);
            var priorityStr = issue.Priority == Priority.None ? "" : issue.Priority.ToString();
            var tagsStr = issue.Tags.Any() ? $":{string.Join(":", issue.Tags)}:" : "";
            var shortId = issue.Id.Replace("task-", "");
            var title = issue.Title.Length > titleWidth - 3 ? issue.Title.Substring(0, titleWidth - 6) + "..." : issue.Title;
            var tags = tagsStr.Length > tagsWidth ? tagsStr.Substring(0, tagsWidth - 3) + "..." : tagsStr;
            _issueDisplayList.Add($"│ {PadRight(stateIcon, stateWidth)} │ {PadRight(shortId, idWidth)} │ {PadRight(priorityStr, prioWidth)} │ {PadRight(title, titleWidth)} │ {PadRight(tags, tagsWidth)} │");
        }
    }

    private string GetStateIcon(IssueState state)
    {
        return state switch
        {
            IssueState.Todo => "TODO",
            IssueState.InProgress => "INPROGRESS",
            IssueState.Done => "DONE",
            IssueState.Kill => "KILL",
            _ => "?"
        };
    }

    private void CreateUI()
    {
        _issueListView = new IssueListView(_issueDisplayList, _issues, ApplyFilter, _showFilterAction ?? (() => ShowFilterDialog()))
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        _issueListView.OpenSelectedItem += (args) =>
        {
            var filteredIssues = GetFilteredIssues();
            if (_issueListView.SelectedItem >= 0 && _issueListView.SelectedItem < filteredIssues.Count)
            {
                var issue = filteredIssues[_issueListView.SelectedItem];
                ShowIssueDetail(issue);
            }
        };

        _tabStatusBar = new StatusBar(new StatusItem[]
        {
            new StatusItem(Key.E, "~E~ Edit", EditIssue),
            new StatusItem(Key.N, "~N~ New", NewIssue),
            new StatusItem(Key.D, "~D~ Mark DONE", MarkAsDone),
            new StatusItem(Key.X, "~X~ Kill", KillIssue),
            new StatusItem((Key)'/', "~/~ Filter", () => { ShowFilterDialog(); })
        })
        {
            X = 0,
            Y = Pos.Bottom(this) - 1,
            Width = Dim.Fill(),
            Height = 1
        };

        Add(_issueListView);
        Add(_tabStatusBar);
    }

    private void ShowFilterDialog()
    {
        var filterDialog = new FilterDialog("Filter Issues", ApplyFilter);
        Application.Run(filterDialog);
    }

    private void ApplyFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            _currentFilter = null;
        }
        else
        {
            _currentFilter = filter;
        }
        UpdateDisplayList();
        _issueListView.SetSource(_issueDisplayList);
    }

    private void NewIssue()
    {
        var editView = new IssueEditView(null);
        editView.SetOnSave((savedIssue) =>
        {
            Refresh();
        });
        Application.Run(editView);
    }

    private void EditIssue()
    {
        var filteredIssues = GetFilteredIssues();
        if (_issueListView.SelectedItem < 0 || _issueListView.SelectedItem >= filteredIssues.Count)
        {
            MessageBox.ErrorQuery("Error", "No issue selected", "OK");
            return;
        }

        var issue = filteredIssues[_issueListView.SelectedItem];
        var editView = new IssueEditView(issue);
        editView.SetOnSave((savedIssue) =>
        {
            Refresh();
        });
        Application.Run(editView);
    }

    private void MarkAsDone()
    {
        var filteredIssues = GetFilteredIssues();
        if (_issueListView.SelectedItem < 0 || _issueListView.SelectedItem >= filteredIssues.Count)
        {
            MessageBox.ErrorQuery("Error", "No issue selected", "OK");
            return;
        }

        var issue = filteredIssues[_issueListView.SelectedItem];
        var confirm = MessageBox.Query("Confirm", $"Mark issue '{issue.Title}' as DONE?", "Yes", "No");
        if (confirm != 0) return;

        try
        {
            var parser = new Parser(OrgiFilePath);
            var allIssues = parser.Parse().ToList();
            var issueIndex = allIssues.FindIndex(i => i.Id == issue.Id);

            if (issueIndex >= 0)
            {
                allIssues[issueIndex] = new Issue(
                    issue.Id,
                    issue.Title,
                    issue.Description,
                    issue.CreatedAt,
                    IssueState.Done,
                    issue.Priority,
                    issue.Tags,
                    issue.Properties
                );

                var content = string.Join("", allIssues.Select(Program.IssueToContent));
                File.WriteAllText(OrgiFilePath, content.TrimStart());
                OrgiWorktree.CommitChanges($"Mark issue {issue.Id} as DONE");
                Refresh();
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Failed to mark issue as DONE: {ex.Message}", "OK");
        }
    }

    private void KillIssue()
    {
        var filteredIssues = GetFilteredIssues();
        if (_issueListView.SelectedItem < 0 || _issueListView.SelectedItem >= filteredIssues.Count)
        {
            MessageBox.ErrorQuery("Error", "No issue selected", "OK");
            return;
        }

        var issue = filteredIssues[_issueListView.SelectedItem];
        var confirm = MessageBox.Query("Confirm", $"Kill issue '{issue.Title}'?", "Yes", "No");
        if (confirm != 0) return;

        try
        {
            var parser = new Parser(OrgiFilePath);
            var allIssues = parser.Parse().ToList();
            var issueIndex = allIssues.FindIndex(i => i.Id == issue.Id);

            if (issueIndex >= 0)
            {
                allIssues[issueIndex] = new Issue(
                    issue.Id,
                    issue.Title,
                    issue.Description,
                    issue.CreatedAt,
                    IssueState.Kill,
                    issue.Priority,
                    issue.Tags,
                    issue.Properties
                );

                var content = string.Join("", allIssues.Select(Program.IssueToContent));
                File.WriteAllText(OrgiFilePath, content.TrimStart());
                OrgiWorktree.CommitChanges($"Kill issue {issue.Id}");
                Refresh();
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Failed to kill issue: {ex.Message}", "OK");
        }
    }

    private void ShowIssueDetail(Issue issue)
    {
        var shortId = issue.Id.Replace("task-", "");
        var detailText = $"ID: {shortId}\n" +
                        $"Title: {issue.Title}\n" +
                        $"State: {issue.State}\n" +
                        $"Priority: {issue.Priority}\n" +
                        $"Created: {issue.CreatedAt}\n" +
                        $"Tags: {string.Join(", ", issue.Tags)}\n" +
                        $"\nDescription:\n{issue.Description}";

        MessageBox.Query("Issue Details", detailText, "Close");
    }

    public override void Refresh()
    {
        LoadIssues();
        _issueListView.SetSource(_issueDisplayList);
    }

    public void ScrollDown()
    {
        if (_issueListView.Source?.Count > 0 && _issueListView.SelectedItem < _issueListView.Source.Count - 1)
        {
            _issueListView.SelectedItem++;
            _issueListView.EnsureSelectedItemVisible();
            _issueListView.SetNeedsDisplay();
        }
    }

    public void ScrollUp()
    {
        if (_issueListView.SelectedItem > 0)
        {
            _issueListView.SelectedItem--;
            _issueListView.EnsureSelectedItemVisible();
            _issueListView.SetNeedsDisplay();
        }
    }
}

public class PRTab : BaseTab
{
    private PRListView _prListView = null!;
    private List<string> _prDisplayList = new();
    private List<PullRequest> _prs = new();
    private string? _currentFilter = null;
    private StatusBar? _tabStatusBar;
    private Action? _showFilterAction;
    private bool _terminalTooSmall = false;

    private class PRListView : ListView
    {
        private List<PullRequest> _allPRs;
        private Action<string> _onFilter;
        private Action _showFilterAction;

        public PRListView(List<string> source, List<PullRequest> allPRs, Action<string> onFilter, Action showFilterAction) : base(source)
        {
            _allPRs = allPRs;
            _onFilter = onFilter;
            _showFilterAction = showFilterAction;
        }

        public override bool ProcessKey(KeyEvent key)
        {
            if (key.KeyValue == (uint)'/')
            {
                _showFilterAction();
                return true;
            }
            return base.ProcessKey(key);
        }

        private void ShowFilterDialog()
        {
            var filterDialog = new FilterDialog("Filter Pull Requests", _onFilter);
            Application.Run(filterDialog);
        }
    }

    public PRTab()
    {
        LoadPRs();
        _showFilterAction = ShowFilterDialog;
        CreateUI();
    }

    private void LoadPRs()
    {
        try
        {
            var prManager = new PRManager();
            _prs = prManager.ListPRs();
            UpdateDisplayList();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Failed to load PRs: {ex.Message}", "OK");
        }
    }

    private const int PRMinTerminalWidth = 80;
    private const int PRStateColumnWidth = 8;
    private const int PRIdColumnWidth = 18;

    private string RepeatChar(char c, int count)
    {
        return new string(c, count);
    }

    private string RepeatString(string s, int count)
    {
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++)
        {
            result.Append(s);
        }
        return result.ToString();
    }

    private string PadLeft(string s, int width)
    {
        return s.PadLeft(width);
    }

    private string PadRight(string s, int width)
    {
        return s.PadRight(width);
    }

    private (int stateWidth, int idWidth, int titleWidth, int statusWidth) CalculatePRColumnWidths()
    {
        var terminalWidth = Application.Top.Frame.Width;
        _terminalTooSmall = terminalWidth < PRMinTerminalWidth;

        if (_terminalTooSmall)
        {
            return (PRStateColumnWidth, PRIdColumnWidth, 20, 8);
        }

        var fixedWidth = PRStateColumnWidth + PRIdColumnWidth + 2;
        var remainingWidth = terminalWidth - fixedWidth - 10;
        var equalColumnWidth = remainingWidth / 2;

        return (PRStateColumnWidth, PRIdColumnWidth, equalColumnWidth, equalColumnWidth);
    }

    private List<PullRequest> GetFilteredPRs()
    {
        if (string.IsNullOrWhiteSpace(_currentFilter))
            return _prs;

        var filter = _currentFilter.ToLower();
        return _prs.Where(pr =>
            pr.Title.ToLower().Contains(filter) ||
            pr.Id.ToLower().Contains(filter) ||
            pr.State.ToString().ToLower().Contains(filter) ||
            pr.Author.ToLower().Contains(filter) ||
            pr.SourceBranch.ToLower().Contains(filter) ||
            pr.TargetBranch.ToLower().Contains(filter)
        ).ToList();
    }

    private void UpdateDisplayList()
    {
        _prDisplayList.Clear();
        var prsToShow = GetFilteredPRs();

        var (stateWidth, idWidth, titleWidth, statusWidth) = CalculatePRColumnWidths();

        if (_terminalTooSmall)
        {
            _prDisplayList.Add("Terminal too small. Please resize to at least 80 columns.");
            return;
        }

        var header = $"│ STATE{RepeatString(" ", stateWidth - 5)}│ ID{RepeatString(" ", idWidth - 2)}│ TITLE{RepeatString(" ", titleWidth - 5)}│ STATUS{RepeatString(" ", statusWidth - 6)}│";
        var separator = $"├{RepeatChar('─', stateWidth + 2)}┼{RepeatChar('─', idWidth + 2)}┼{RepeatChar('─', titleWidth + 2)}┼{RepeatChar('─', statusWidth + 2)}┤";
        _prDisplayList.Add(header);
        _prDisplayList.Add(separator);

        foreach (var pr in prsToShow)
        {
            var stateIcon = GetStateIcon(pr.State);
            var approvalStatus = pr.IsApproved ? "APPROVED" : pr.IsDenied ? "DENIED" : "PENDING";
            var shortId = pr.Id.Replace("pr-", "");
            var title = pr.Title.Length > titleWidth - 3 ? pr.Title.Substring(0, titleWidth - 6) + "..." : pr.Title;
            _prDisplayList.Add($"│ {PadRight(stateIcon, stateWidth)} │ {PadRight(shortId, idWidth)} │ {PadRight(title, titleWidth)} │ {PadRight(approvalStatus, statusWidth)} │");
        }
    }

    private string GetStateIcon(PullRequestState state)
    {
        return state switch
        {
            PullRequestState.Open => "OPEN",
            PullRequestState.Approved => "APPROVED",
            PullRequestState.Denied => "DENIED",
            PullRequestState.Merged => "MERGED",
            _ => "?"
        };
    }

    private void CreateUI()
    {
        _prListView = new PRListView(_prDisplayList, _prs, ApplyFilter, _showFilterAction ?? (() => ShowFilterDialog()))
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        _prListView.OpenSelectedItem += (args) =>
        {
            var filteredPRs = GetFilteredPRs();
            if (_prListView.SelectedItem >= 0 && _prListView.SelectedItem < filteredPRs.Count)
            {
                var pr = filteredPRs[_prListView.SelectedItem];
                ShowPRDetail(pr);
            }
        };

        _tabStatusBar = new StatusBar(new StatusItem[]
        {
            new StatusItem((Key)'/', "~/~ Filter", () => { ShowFilterDialog(); })
        })
        {
            X = 0,
            Y = Pos.Bottom(this) - 1,
            Width = Dim.Fill(),
            Height = 1
        };

        Add(_prListView);
        Add(_tabStatusBar);
    }

    private void ShowFilterDialog()
    {
        var filterDialog = new FilterDialog("Filter Pull Requests", ApplyFilter);
        Application.Run(filterDialog);
    }

    private void ApplyFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            _currentFilter = null;
        }
        else
        {
            _currentFilter = filter;
        }
        UpdateDisplayList();
        _prListView.SetSource(_prDisplayList);
    }

    private void ShowPRDetail(PullRequest pr)
    {
        var reviewsText = pr.Reviews.Any()
            ? string.Join("\n", pr.Reviews.Select(r => $"  - {r.Reviewer}: {(r.Approved ? "Approved" : "Denied")}{(r.Comment != null ? $" ({r.Comment})" : "")}"))
            : "  No reviews";

        var shortId = pr.Id.Replace("pr-", "");
        var detailText = $"ID: {shortId}\n" +
                        $"Title: {pr.Title}\n" +
                        $"Description: {pr.Description}\n" +
                        $"Author: {pr.Author}\n" +
                        $"Created: {pr.CreatedAt}\n" +
                        $"State: {pr.State}\n" +
                        $"Branch: {pr.SourceBranch} → {pr.TargetBranch}\n" +
                        $"\nReviews:\n{reviewsText}";

        MessageBox.Query("PR Details", detailText, "Close");
    }

    public override void Refresh()
    {
        LoadPRs();
        _prListView.SetSource(_prDisplayList);
    }

    public void ScrollDown()
    {
        if (_prListView.Source?.Count > 0 && _prListView.SelectedItem < _prListView.Source.Count - 1)
        {
            _prListView.SelectedItem++;
            _prListView.EnsureSelectedItemVisible();
            _prListView.SetNeedsDisplay();
        }
    }

    public void ScrollUp()
    {
        if (_prListView.SelectedItem > 0)
        {
            _prListView.SelectedItem--;
            _prListView.EnsureSelectedItemVisible();
            _prListView.SetNeedsDisplay();
        }
    }
}

public class FilterDialog : Dialog
{
    private TextField _filterField = null!;
    private Action<string> _onFilter;

    public FilterDialog(string title, Action<string> onFilter)
    {
        Title = title;
        _onFilter = onFilter;
        X = Pos.Center();
        Y = Pos.Center();
        Width = 50;
        Height = 8;
        ColorScheme = Colors.Base;

        CreateUI();
    }

    private void CreateUI()
    {
        new Label(1, 1, "Filter:");
        _filterField = new TextField("")
        {
            X = 9,
            Y = 1,
            Width = Dim.Fill() - 2
        };
        Add(_filterField);

        var filterButton = new Button("Filter")
        {
            X = Pos.Center() - 10,
            Y = 4,
            Width = 8
        };
        filterButton.Clicked += () =>
        {
            _onFilter(_filterField.Text.ToString());
            Application.RequestStop();
        };
        Add(filterButton);

        var clearButton = new Button("Clear")
        {
            X = Pos.Center() + 2,
            Y = 4,
            Width = 8
        };
        clearButton.Clicked += () =>
        {
            _onFilter("");
            Application.RequestStop();
        };
        Add(clearButton);

        var cancelButton = new Button("Cancel")
        {
            X = Pos.Right(clearButton) + 2,
            Y = 4,
            Width = 8
        };
        cancelButton.Clicked += () => { Application.RequestStop(); };
        Add(cancelButton);

        _filterField.SetFocus();
    }
}
