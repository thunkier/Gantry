using System.Collections.Generic;

namespace Gantry.Core.Interfaces;

public enum GitFileStatus
{
    Unmodified,
    Added,
    Modified,
    Deleted,
    Renamed,
    Copied,
    Untracked,
    Ignored
}

public class GitFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public GitFileStatus Status { get; set; }
    public bool IsStaged { get; set; }
    public string? OldFilePath { get; set; } // For renamed files
}

public class GitCommitInfo
{
    public string Sha { get; set; } = string.Empty;
    public string ShortSha { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ShortMessage { get; set; } = string.Empty;
}

public class GitBranchInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public bool IsRemote { get; set; }
}

public class GitStashInfo
{
    public int Index { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface IGitService
{
    // Repository operations
    bool IsGitRepo(string path);
    void Initialize(string path);
    void Initialize(string path, bool gitIgnore, bool readme);

    // File status operations
    IEnumerable<string> GetChangedFiles(string path);
    IEnumerable<GitFileInfo> GetFileStatus(string path);
    IEnumerable<GitFileInfo> GetStagedFiles(string path);
    IEnumerable<GitFileInfo> GetUnstagedFiles(string path);

    // Staging operations
    void Stage(string path, string file);
    void Unstage(string path, string file);
    void StageAll(string path);
    void UnstageAll(string path);
    void DiscardChanges(string path, string file);

    // Diff operations
    string GetDiff(string path, string file, bool staged = false);

    // Commit operations
    void Commit(string path, string message);
    IEnumerable<GitCommitInfo> GetCommitHistory(string path, int count = 50);

    // Branch operations
    IEnumerable<GitBranchInfo> GetBranches(string path);
    string GetCurrentBranch(string path);
    void CreateBranch(string path, string branchName);
    void CheckoutBranch(string path, string branchName);
    void DeleteBranch(string path, string branchName, bool force = false);

    // Remote operations
    void Push(string path, string remote, string branch);
    void Pull(string path, string remote, string branch);
    void Fetch(string path, string remote);
    void AddRemote(string path, string name, string url);
    IEnumerable<string> GetRemotes(string path);

    // Stash operations
    void Stash(string path, string message);
    IEnumerable<GitStashInfo> GetStashes(string path);
    void ApplyStash(string path, int index);
    void PopStash(string path, int index);
    void DropStash(string path, int index);
}
