using System.Diagnostics;
using Orgi.Core.Model;

namespace Orgi.Core;

public class PRManager
{
    private const string PrBranchName = "orgi-prs";
    private const string OrgiDir = ".orgi";
    private readonly string _worktreePath;

    public PRManager()
    {
        _worktreePath = Path.Combine(Directory.GetCurrentDirectory(), OrgiDir);
    }

    /// <summary>
    /// Ensures the PR branch and worktree are set up
    /// </summary>
    public void EnsurePrSetup()
    {
        if (!IsGitRepository())
        {
            throw new InvalidOperationException("Not a git repository");
        }

        if (!BranchExists(PrBranchName))
        {
            CreatePrBranch();
        }

        if (!WorktreeExists())
        {
            CreateWorktree();
        }
    }

    /// <summary>
    /// Creates a new PR
    /// </summary>
    public PullRequest CreatePR(string title, string description, string sourceBranch, string targetBranch = "main")
    {
        EnsurePrSetup();

        var pr = new PullRequest(
            PullRequest.GenerateId(),
            title,
            description,
            GetCurrentUser(),
            sourceBranch,
            targetBranch);

        SavePR(pr);
        PushPrBranch();

        return pr;
    }

    /// <summary>
    /// Lists all PRs
    /// </summary>
    public List<PullRequest> ListPRs()
    {
        EnsurePrSetup();

        var prDir = Path.Combine(_worktreePath, "prs");
        if (!Directory.Exists(prDir))
        {
            return new List<PullRequest>();
        }

        var prs = new List<PullRequest>();
        foreach (var file in Directory.GetFiles(prDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var pr = PullRequest.FromJson(json);
                prs.Add(pr);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading PR from {file}: {ex.Message}");
            }
        }

        return prs.OrderByDescending(p => p.CreatedAt).ToList();
    }

    /// <summary>
    /// Approves a PR
    /// </summary>
    public void ApprovePR(string prId, string reviewer, string? comment = null)
    {
        var pr = GetPR(prId);
        pr.Reviews.Add(new PullRequestReview(reviewer, true, comment));
        pr.State = PullRequestState.Approved;
        pr.UpdatedAt = DateTime.Now;

        SavePR(pr);
        PushPrBranch();
    }

    /// <summary>
    /// Denies a PR
    /// </summary>
    public void DenyPR(string prId, string reviewer, string? comment = null)
    {
        var pr = GetPR(prId);
        pr.Reviews.Add(new PullRequestReview(reviewer, false, comment));
        pr.State = PullRequestState.Denied;
        pr.UpdatedAt = DateTime.Now;

        SavePR(pr);
        PushPrBranch();
    }

    /// <summary>
    /// Merges a PR
    /// </summary>
    public void MergePR(string prId, string merger)
    {
        var pr = GetPR(prId);
        if (!pr.CanMerge)
        {
            throw new InvalidOperationException("PR cannot be merged - not approved or has denials");
        }

        // Perform merge
        var mergeCommit = PerformMerge(pr.SourceBranch, pr.TargetBranch);

        pr.State = PullRequestState.Merged;
        pr.MergedBy = merger;
        pr.MergedAt = DateTime.Now;
        pr.MergeCommit = mergeCommit;
        pr.UpdatedAt = DateTime.Now;

        SavePR(pr);
        LogMerge(pr);
        PushPrBranch();
    }

    private PullRequest GetPR(string prId)
    {
        var prFile = Path.Combine(_worktreePath, "prs", $"{prId}.json");
        if (!File.Exists(prFile))
        {
            throw new FileNotFoundException($"PR {prId} not found");
        }

        var json = File.ReadAllText(prFile);
        return PullRequest.FromJson(json);
    }

    private void SavePR(PullRequest pr)
    {
        var prDir = Path.Combine(_worktreePath, "prs");
        Directory.CreateDirectory(prDir);

        var prFile = Path.Combine(prDir, $"{pr.Id}.json");
        File.WriteAllText(prFile, pr.ToJson());
    }

    private void LogMerge(PullRequest pr)
    {
        var logDir = Path.Combine(_worktreePath, "logs");
        Directory.CreateDirectory(logDir);

        var logFile = Path.Combine(logDir, $"{pr.Id}-merge.log");
        var log = $"PR {pr.Id} merged by {pr.MergedBy} at {pr.MergedAt}\n" +
                 $"Title: {pr.Title}\n" +
                 $"Source: {pr.SourceBranch} -> Target: {pr.TargetBranch}\n" +
                 $"Merge commit: {pr.MergeCommit}\n" +
                 $"Reviews: {pr.Reviews.Count} ({pr.Reviews.Count(r => r.Approved)} approved, {pr.Reviews.Count(r => !r.Approved)} denied)\n";

        File.WriteAllText(logFile, log);
    }

    private bool IsGitRepository()
    {
        return RunGitCommand("rev-parse --git-dir").Success;
    }

    private bool BranchExists(string branchName)
    {
        var result = RunGitCommand($"branch --list {branchName}");
        return result.Success && !string.IsNullOrWhiteSpace(result.Output);
    }

    private bool WorktreeExists()
    {
        var result = RunGitCommand("worktree list");
        return result.Success && result.Output.Contains(_worktreePath);
    }

    private void CreatePrBranch()
    {
        RunGitCommand($"checkout -b {PrBranchName}", throwOnError: true);
        RunGitCommand("checkout -", throwOnError: true); // Go back to original branch
    }

    private void CreateWorktree()
    {
        // Remove existing directory if it exists (from init command)
        if (Directory.Exists(_worktreePath))
        {
            // Check if directory is empty or only contains orgi.org
            var files = Directory.GetFiles(_worktreePath);
            var dirs = Directory.GetDirectories(_worktreePath);
            if (files.Length <= 1 && dirs.Length == 0 && (files.Length == 0 || files[0].EndsWith("orgi.org")))
            {
                Directory.Delete(_worktreePath, true);
            }
            else
            {
                throw new InvalidOperationException($"Directory {_worktreePath} exists and contains unexpected files. Please remove it manually.");
            }
        }
        RunGitCommand($"worktree add {_worktreePath} {PrBranchName}", throwOnError: true);
    }

    private void PushPrBranch()
    {
        RunGitCommand($"push origin {PrBranchName}");
    }

    private string PerformMerge(string sourceBranch, string targetBranch)
    {
        // Checkout target branch
        RunGitCommand($"checkout {targetBranch}", throwOnError: true);
        
        // Merge source branch
        var mergeResult = RunGitCommand($"merge {sourceBranch} --no-ff -m \"Merge PR: {sourceBranch}\"", throwOnError: true);
        
        // Get merge commit hash
        var commitResult = RunGitCommand("rev-parse HEAD");
        var mergeCommit = commitResult.Output.Trim();

        // Push the merge
        RunGitCommand("push", throwOnError: true);

        return mergeCommit;
    }

    private string GetCurrentUser()
    {
        var result = RunGitCommand("config user.name");
        return result.Success ? result.Output.Trim() : Environment.UserName;
    }

    private (bool Success, string Output) RunGitCommand(string args, bool throwOnError = false)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            if (throwOnError)
                throw new InvalidOperationException($"Failed to start git command: {args}");
            return (false, "");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var success = process.ExitCode == 0;
        var result = success ? output : error;

        if (!success && throwOnError)
        {
            throw new InvalidOperationException($"Git command failed: git {args}\n{result}");
        }

        return (success, result);
    }
}
