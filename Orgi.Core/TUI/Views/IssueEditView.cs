using Terminal.Gui;
using NStack;
using Orgi.Core.Model;
using Orgi.Core.Parsing;
using Orgi.Core;
using System.Collections;

namespace Orgi.Core.TUI;

public class ScrollableTextView : TextView
{
    public ScrollableTextView() : base()
    {
    }

    public ScrollableTextView(Rect frame) : base(frame)
    {
    }

    public override bool ProcessKey(KeyEvent key)
    {
        if (key.KeyValue == (uint)'j')
        {
            base.ProcessKey(new KeyEvent(Key.CursorDown, new KeyModifiers()));
            return true;
        }
        if (key.KeyValue == (uint)'k')
        {
            base.ProcessKey(new KeyEvent(Key.CursorUp, new KeyModifiers()));
            return true;
        }
        return base.ProcessKey(key);
    }
}

public class ScrollableListView : ListView
{
    public ScrollableListView(IList source) : base(source)
    {
    }

    public ScrollableListView(IListDataSource source) : base(source)
    {
    }

    public ScrollableListView() : base()
    {
    }

    public ScrollableListView(Rect frame, IList source) : base(frame, source)
    {
    }

    public ScrollableListView(Rect frame, IListDataSource source) : base(frame, source)
    {
    }

    public override bool ProcessKey(KeyEvent key)
    {
        if (key.KeyValue == (uint)'j')
        {
            if (Source?.Count > 0 && SelectedItem < Source.Count - 1)
            {
                SelectedItem++;
                EnsureSelectedItemVisible();
                SetNeedsDisplay();
            }
            return true;
        }
        if (key.KeyValue == (uint)'k')
        {
            if (SelectedItem > 0)
            {
                SelectedItem--;
                EnsureSelectedItemVisible();
                SetNeedsDisplay();
            }
            return true;
        }
        return base.ProcessKey(key);
    }
}

public class IssueEditView : Dialog
{
    private TextField _titleField = null!;
    private ScrollableListView _stateList = null!;
    private ScrollableListView _priorityList = null!;
    private TextField _tagsField = null!;
    private ScrollableTextView _descriptionView = null!;
    private Issue? _issue;
    private bool _isEditMode;
    private Action<Issue>? _onSave;

    public IssueEditView(Issue? issue = null)
    {
        _issue = issue;
        _isEditMode = issue != null;

        Title = _isEditMode ? "Edit Issue" : "New Issue";
        X = Pos.Center();
        Y = Pos.Center();
        Width = Dim.Percent(80);
        Height = Dim.Percent(80);
        ColorScheme = Colors.Base;

        CreateUI();
    }

    public override bool ProcessKey(KeyEvent key)
    {
        if (key.KeyValue == (uint)'j' && _descriptionView.HasFocus)
        {
            _descriptionView.ProcessKey(new KeyEvent(Key.CursorDown, new KeyModifiers()));
            return true;
        }
        if (key.KeyValue == (uint)'k' && _descriptionView.HasFocus)
        {
            _descriptionView.ProcessKey(new KeyEvent(Key.CursorUp, new KeyModifiers()));
            return true;
        }
        return base.ProcessKey(key);
    }

    private void CreateUI()
    {
        var y = 1;

        new Label(1, y, "Title:");
        _titleField = new TextField("")
        {
            X = 20,
            Y = y,
            Width = Dim.Fill() - 2
        };
        Add(_titleField);
        y += 2;

        new Label(1, y, "State:");
        _stateList = new ScrollableListView(new List<string> { "TODO", "INPROGRESS", "DONE", "KILL" })
        {
            X = 20,
            Y = y,
            Width = 20,
            Height = 4
        };
        Add(_stateList);
        y += 5;

        new Label(1, y, "Priority:");
        _priorityList = new ScrollableListView(new List<string> { "None", "A", "B", "C" })
        {
            X = 20,
            Y = y,
            Width = 10,
            Height = 4
        };
        Add(_priorityList);
        y += 5;

        new Label(1, y, "Tags (comma-separated):");
        _tagsField = new TextField("")
        {
            X = 30,
            Y = y,
            Width = Dim.Fill() - 2
        };
        Add(_tagsField);
        y += 2;

        new Label(1, y, "Description:");
        y += 1;
        _descriptionView = new ScrollableTextView()
        {
            X = 1,
            Y = y,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 8
        };
        Add(_descriptionView);

        var buttons = new View()
        {
            X = Pos.Center(),
            Y = Pos.Bottom(this) - 3,
            Width = 20,
            Height = 1
        };

        var saveButton = new Button("Save")
        {
            X = 0,
            Y = 0,
            Width = 8
        };
        saveButton.Clicked += OnSave;
        buttons.Add(saveButton);

        var cancelButton = new Button("Cancel")
        {
            X = 12,
            Y = 0,
            Width = 8
        };
        cancelButton.Clicked += () => { Application.RequestStop(); };
        buttons.Add(cancelButton);

        Add(buttons);

        if (_isEditMode && _issue != null)
        {
            LoadIssueData();
        }
        else
        {
            _stateList.SelectedItem = 0;
            _priorityList.SelectedItem = 0;
        }
    }

    private void LoadIssueData()
    {
        if (_issue == null) return;

        _titleField.Text = ustring.Make(_issue.Title);
        _tagsField.Text = ustring.Make(string.Join(", ", _issue.Tags));
        _descriptionView.Text = ustring.Make(_issue.Description);

        var stateIndex = _issue.State switch
        {
            IssueState.Todo => 0,
            IssueState.InProgress => 1,
            IssueState.Done => 2,
            IssueState.Kill => 3,
            _ => 0
        };
        _stateList.SelectedItem = stateIndex;

        var priorityIndex = _issue.Priority switch
        {
            Priority.A => 1,
            Priority.B => 2,
            Priority.C => 3,
            _ => 0
        };
        _priorityList.SelectedItem = priorityIndex;
    }

    private void OnSave()
    {
        var titleText = _titleField.Text.ToString();
        if (string.IsNullOrWhiteSpace(titleText))
        {
            MessageBox.ErrorQuery("Error", "Title is required", "OK");
            return;
        }

        try
        {
            var parser = new Parser(".orgi/orgi.org");
            var allIssues = parser.Parse().ToList();

            var state = _stateList.SelectedItem switch
            {
                0 => IssueState.Todo,
                1 => IssueState.InProgress,
                2 => IssueState.Done,
                3 => IssueState.Kill,
                _ => IssueState.Todo
            };

            var priority = _priorityList.SelectedItem switch
            {
                1 => Priority.A,
                2 => Priority.B,
                3 => Priority.C,
                _ => Priority.None
            };

            var tagsText = _tagsField.Text.ToString();
            var tags = tagsText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            var descriptionText = _descriptionView.Text.ToString() ?? "";

            if (_isEditMode && _issue != null)
            {
                var issueIndex = allIssues.FindIndex(i => i.Id == _issue.Id);
                if (issueIndex >= 0)
                {
                    var updatedIssue = new Issue(
                        _issue.Id,
                        titleText,
                        descriptionText,
                        _issue.CreatedAt,
                        state,
                        priority,
                        tags,
                        _issue.Properties
                    );
                    allIssues[issueIndex] = updatedIssue;
                }
            }
            else
            {
                var newId = "task-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var created = DateTime.Now;
                var properties = new Dictionary<string, string>
                {
                    { "ID", newId },
                    { "TITLE", titleText },
                    { "CREATED", created.ToString("<yyyy-MM-dd ddd HH:mm>") }
                };

                var newIssue = new Issue(
                    newId,
                    titleText,
                    descriptionText,
                    created,
                    state,
                    priority,
                    tags,
                    properties
                );
                allIssues.Add(newIssue);
            }

            var content = string.Join("", allIssues.Select(Program.IssueToContent));
            File.WriteAllText(".orgi/orgi.org", content.TrimStart());
            OrgiWorktree.CommitChanges(_isEditMode ? "Update issue" : "Add issue");

            _onSave?.Invoke(_issue ?? allIssues.Last());
            Application.RequestStop();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Failed to save issue: {ex.Message}", "OK");
        }
    }

    public void SetOnSave(Action<Issue> onSave)
    {
        _onSave = onSave;
    }
}
