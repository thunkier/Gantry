using Gantry.Core.Interfaces;
using LibGit2Sharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Gantry.Infrastructure.Services;

public class GitService : IGitService
{
    public bool IsGitRepo(string path)
    {
        return Repository.IsValid(path);
    }

    public void Initialize(string path)
    {
        Repository.Init(path);
    }

    public void Initialize(string path, bool gitIgnore, bool readme)
    {
        Repository.Init(path);

        if (gitIgnore)
        {
            File.WriteAllText(Path.Combine(path, ".gitignore"), "bin/\nobj/\n.vs/\n");
        }

        if (readme)
        {
            File.WriteAllText(Path.Combine(path, "README.md"), $"# {new DirectoryInfo(path).Name}\n");
        }

        if (gitIgnore || readme)
        {
            using var repo = new Repository(path);
            if (gitIgnore) Commands.Stage(repo, ".gitignore");
            if (readme) Commands.Stage(repo, "README.md");

            var signature = repo.Config.BuildSignature(System.DateTimeOffset.Now);
            if (signature != null)
            {
                repo.Commit("Initial commit", signature, signature);
            }
        }
    }

    public IEnumerable<string> GetChangedFiles(string path)
    {
        if (!IsGitRepo(path)) return Enumerable.Empty<string>();

        using var repo = new Repository(path);
        var status = repo.RetrieveStatus();

        return status.Where(s => s.State != FileStatus.Ignored)
                     .Select(s => s.FilePath);
    }

    public IEnumerable<GitFileInfo> GetFileStatus(string path)
    {
        if (!IsGitRepo(path)) return Enumerable.Empty<GitFileInfo>();

        using var repo = new Repository(path);
        var status = repo.RetrieveStatus();

        return status.Select(s => new GitFileInfo
        {
            FilePath = s.FilePath,
            Status = MapFileStatus(s.State),
            IsStaged = IsFileStaged(s.State)
        }).Where(f => f.Status != GitFileStatus.Ignored && f.Status != GitFileStatus.Unmodified);
    }

    public IEnumerable<GitFileInfo> GetStagedFiles(string path)
    {
        return GetFileStatus(path).Where(f => f.IsStaged);
    }

    public IEnumerable<GitFileInfo> GetUnstagedFiles(string path)
    {
        return GetFileStatus(path).Where(f => !f.IsStaged);
    }

    public void Stage(string path, string file)
    {
        using var repo = new Repository(path);
        Commands.Stage(repo, file);
    }

    public void Unstage(string path, string file)
    {
        using var repo = new Repository(path);
        Commands.Unstage(repo, file);
    }

    public void StageAll(string path)
    {
        using var repo = new Repository(path);
        Commands.Stage(repo, "*");
    }

    public void UnstageAll(string path)
    {
        using var repo = new Repository(path);
        Commands.Unstage(repo, "*");
    }

    public void DiscardChanges(string path, string file)
    {
        using var repo = new Repository(path);
        var options = new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force };
        repo.CheckoutPaths("HEAD", new[] { file }, options);
    }

    public string GetDiff(string path, string file, bool staged = false)
    {
        if (!IsGitRepo(path)) return string.Empty;

        using var repo = new Repository(path);

        var patch = staged
            ? repo.Diff.Compare<Patch>(repo.Head.Tip?.Tree, DiffTargets.Index, new[] { file })
            : repo.Diff.Compare<Patch>(new[] { file }, true);

        return patch?.Content ?? string.Empty;
    }

    public void Commit(string path, string message)
    {
        using var repo = new Repository(path);
        var signature = repo.Config.BuildSignature(System.DateTimeOffset.Now);
        if (signature == null)
        {
            throw new LibGit2SharpException("Git identity not set. Please configure user.name and user.email.");
        }
        repo.Commit(message, signature, signature);
    }

    public IEnumerable<GitCommitInfo> GetCommitHistory(string path, int count = 50)
    {
        if (!IsGitRepo(path)) return Enumerable.Empty<GitCommitInfo>();

        using var repo = new Repository(path);

        return repo.Commits
            .Take(count)
            .Select(c => new GitCommitInfo
            {
                Sha = c.Sha,
                ShortSha = c.Sha.Substring(0, 7),
                Author = c.Author.Name,
                Email = c.Author.Email,
                Date = c.Author.When,
                Message = c.Message,
                ShortMessage = c.MessageShort
            })
            .ToList();
    }

    public IEnumerable<GitBranchInfo> GetBranches(string path)
    {
        if (!IsGitRepo(path)) return Enumerable.Empty<GitBranchInfo>();

        using var repo = new Repository(path);
        var currentBranch = repo.Head.FriendlyName;

        return repo.Branches.Select(b => new GitBranchInfo
        {
            Name = b.FriendlyName,
            IsCurrent = b.FriendlyName == currentBranch,
            IsRemote = b.IsRemote
        }).ToList();
    }

    public string GetCurrentBranch(string path)
    {
        if (!IsGitRepo(path)) return string.Empty;

        using var repo = new Repository(path);
        return repo.Head.FriendlyName;
    }

    public void CreateBranch(string path, string branchName)
    {
        using var repo = new Repository(path);
        repo.CreateBranch(branchName);
    }

    public void CheckoutBranch(string path, string branchName)
    {
        using var repo = new Repository(path);
        var branch = repo.Branches[branchName];
        if (branch != null)
        {
            Commands.Checkout(repo, branch);
        }
    }

    public void DeleteBranch(string path, string branchName, bool force = false)
    {
        using var repo = new Repository(path);
        repo.Branches.Remove(branchName);
    }

    public void Push(string path, string remote, string branch)
    {
        using var repo = new Repository(path);
        var remoteObj = repo.Network.Remotes[remote];
        if (remoteObj == null) return;

        repo.Network.Push(remoteObj, $"refs/heads/{branch}", new PushOptions());
    }

    public void Pull(string path, string remote, string branch)
    {
        using var repo = new Repository(path);
        var signature = repo.Config.BuildSignature(System.DateTimeOffset.Now);
        if (signature == null) return;

        Commands.Pull(repo, new Signature(signature.Name, signature.Email, System.DateTimeOffset.Now), new PullOptions());
    }

    public void Fetch(string path, string remote)
    {
        using var repo = new Repository(path);
        var remoteObj = repo.Network.Remotes[remote];
        if (remoteObj == null) return;

        Commands.Fetch(repo, remoteObj.Name, Array.Empty<string>(), new FetchOptions(), null);
    }

    public void AddRemote(string path, string name, string url)
    {
        using var repo = new Repository(path);
        repo.Network.Remotes.Add(name, url);
    }

    public IEnumerable<string> GetRemotes(string path)
    {
        using var repo = new Repository(path);
        return repo.Network.Remotes.Select(r => r.Name).ToList();
    }

    public void Stash(string path, string message)
    {
        using var repo = new Repository(path);
        var signature = repo.Config.BuildSignature(System.DateTimeOffset.Now);
        if (signature == null)
        {
            throw new LibGit2SharpException("Git identity not set. Please configure user.name and user.email.");
        }
        repo.Stashes.Add(signature, message);
    }

    public IEnumerable<GitStashInfo> GetStashes(string path)
    {
        if (!IsGitRepo(path)) return Enumerable.Empty<GitStashInfo>();

        using var repo = new Repository(path);
        return repo.Stashes.Select((s, index) => new GitStashInfo
        {
            Index = index,
            Message = s.Message
        }).ToList();
    }

    public void ApplyStash(string path, int index)
    {
        using var repo = new Repository(path);
        var stash = repo.Stashes.ElementAtOrDefault(index);
        if (stash != null)
        {
            repo.Stashes.Apply(index);
        }
    }

    public void PopStash(string path, int index)
    {
        using var repo = new Repository(path);
        var stash = repo.Stashes.ElementAtOrDefault(index);
        if (stash != null)
        {
            repo.Stashes.Pop(index);
        }
    }

    public void DropStash(string path, int index)
    {
        using var repo = new Repository(path);
        repo.Stashes.Remove(index);
    }

    // Helper methods
    private static GitFileStatus MapFileStatus(FileStatus status)
    {
        return status switch
        {
            FileStatus.NewInIndex or FileStatus.NewInWorkdir => GitFileStatus.Added,
            FileStatus.ModifiedInIndex or FileStatus.ModifiedInWorkdir => GitFileStatus.Modified,
            FileStatus.DeletedFromIndex or FileStatus.DeletedFromWorkdir => GitFileStatus.Deleted,
            FileStatus.RenamedInIndex or FileStatus.RenamedInWorkdir => GitFileStatus.Renamed,
            FileStatus.TypeChangeInIndex or FileStatus.TypeChangeInWorkdir => GitFileStatus.Modified,
            FileStatus.Ignored => GitFileStatus.Ignored,
            FileStatus.Unreadable => GitFileStatus.Untracked,
            _ => GitFileStatus.Unmodified
        };
    }

    private static bool IsFileStaged(FileStatus status)
    {
        return status.HasFlag(FileStatus.NewInIndex) ||
               status.HasFlag(FileStatus.ModifiedInIndex) ||
               status.HasFlag(FileStatus.DeletedFromIndex) ||
               status.HasFlag(FileStatus.RenamedInIndex) ||
               status.HasFlag(FileStatus.TypeChangeInIndex);
    }
}
