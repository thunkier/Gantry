using System.Text.Json;
using Gantry.Core.Domain.Settings;
using Gantry.Core.Domain.Workspaces;
using Gantry.Infrastructure.Serialization;

namespace Gantry.Infrastructure.Services;

public class WorkspaceService
{
    private readonly string _configPath;
    private AppSessionState _sessionState = new();

    // Event to notify UI when the active workspace changes
    public event EventHandler<Workspace>? WorkspaceChanged;

    public Workspace? CurrentWorkspace { get; private set; }
    public IReadOnlyList<string> RecentWorkspaces => _sessionState.RecentWorkspaces;
    public Gantry.Infrastructure.Persistence.FileSystemCollectionRepository Repository { get; } = new();

    public WorkspaceService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var gantryDir = Path.Combine(appData, "Gantry");

        if (!Directory.Exists(gantryDir)) Directory.CreateDirectory(gantryDir);

        _configPath = Path.Combine(gantryDir, "session.json");
        FileSystemChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task InitializeAsync()
    {
        await LoadStateAsync();

        // Restore last session if it exists and still is valid
        if (!string.IsNullOrEmpty(_sessionState.LastActiveWorkspacePath) &&
            Directory.Exists(_sessionState.LastActiveWorkspacePath))
        {
            OpenWorkspace(_sessionState.LastActiveWorkspacePath);
        }
    }

    private FileSystemWatcher? _watcher;
    public event EventHandler? FileSystemChanged;

    private void SetupWatcher(string path)
    {
        _watcher?.Dispose();
        _watcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileSystemChanged;
        _watcher.Deleted += OnFileSystemChanged;
        _watcher.Renamed += OnFileSystemChanged;
        // _watcher.Changed += OnFileSystemChanged; // Often too noisy
    }

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce or throttle could be added here if needed
        // For now, just notify on UI thread if possible, or let ViewModel handle marshalling
        FileSystemChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OpenWorkspace(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;

        // 1. Set Active Workspace
        var name = new DirectoryInfo(path).Name;
        CurrentWorkspace = new Workspace { Name = name, Path = path };

        // 2. Update MRU (Most Recently Used) List
        // Remove if exists (to move it to top), then insert at 0
        _sessionState.RecentWorkspaces.RemoveAll(x => x.Equals(path, StringComparison.OrdinalIgnoreCase));
        _sessionState.RecentWorkspaces.Insert(0, path);

        // Keep history manageable (e.g., max 10 items)
        if (_sessionState.RecentWorkspaces.Count > 10)
        {
            _sessionState.RecentWorkspaces = _sessionState.RecentWorkspaces.Take(10).ToList();
        }

        // 3. Update Session State
        _sessionState.LastActiveWorkspacePath = path;

        // 4. Persist and Notify
        SaveState();
        WorkspaceChanged?.Invoke(this, CurrentWorkspace);
        
        // 5. Setup File Watcher
        SetupWatcher(path);
    }

    public void ClearHistory()
    {
        _sessionState.RecentWorkspaces.Clear();
        SaveState();
    }

    private async Task LoadStateAsync()
    {
        if (!File.Exists(_configPath)) return;

        try
        {
            var json = await File.ReadAllTextAsync(_configPath);
            var state = JsonSerializer.Deserialize(json, WorkspaceJsonContext.Default.AppSessionState);

            if (state != null)
            {
                _sessionState = state;
                // Validate history (remove paths that no longer exist on disk)
                _sessionState.RecentWorkspaces = _sessionState.RecentWorkspaces
                    .Where(Directory.Exists)
                    .ToList();
            }
        }
        catch
        {
            // Corrupt config, start fresh
            _sessionState = new AppSessionState();
        }
    }

    private void SaveState()
    {
        // Fire and forget save, or await it if you prefer strict persistence guarantees
        var json = JsonSerializer.Serialize(_sessionState, WorkspaceJsonContext.Default.AppSessionState);
        File.WriteAllText(_configPath, json);
    }
}