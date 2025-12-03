using System;
using System.IO;
using Gantry.Core.Domain.Collections;
using Gantry.UI.Features.Collections.ViewModels;
using Gantry.UI.Interfaces;

namespace Gantry.UI.Features.Collections.Services;

public class CollectionService
{
    private readonly Gantry.Infrastructure.Services.WorkspaceService _workspaceService;

    public CollectionService(Gantry.Infrastructure.Services.WorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    public CollectionViewModel CreateCollection(string parentPath, string baseName = "New Collection")
    {
        var newPath = Path.Combine(parentPath, baseName);
        int i = 1;
        while (Directory.Exists(newPath))
        {
            newPath = Path.Combine(parentPath, $"{baseName} {i++}");
        }

        Directory.CreateDirectory(newPath);

        // We don't return the VM here because the caller might want to reload or add it manually.
        // But to be helpful, let's return the model or VM.
        // The original code reloaded the whole collection list for root collections, 
        // but for sub-collections it added to children.
        // Let's return the Model so the VM can wrap it.

        return new CollectionViewModel(new Collection
        {
            Name = Path.GetFileName(newPath),
            Path = newPath
        }, _workspaceService);
    }

    private readonly Gantry.Infrastructure.Persistence.RequestBundleRepository _bundleRepository = new();

    public RequestItemViewModel CreateRequest(CollectionViewModel parent)
    {
        var baseName = "New Request.req";
        var newPath = Path.Combine(parent.Model.Path, baseName);

        if (Directory.Exists(newPath) || File.Exists(newPath))
        {
            newPath = ResolveNameConflict(newPath, baseName);
        }

        // Create bundle directory
        Directory.CreateDirectory(newPath);

        var request = new RequestItem
        {
            Name = Path.GetFileNameWithoutExtension(newPath),
            Path = newPath,
            Request = new Gantry.Core.Domain.Http.RequestModel { Method = "GET", Url = "http://localhost" },
            Parent = parent.Model
        };

        // Save initial state
        _bundleRepository.SaveBundle(request);

        return new RequestItemViewModel(request);
    }

    public CollectionViewModel CreateFolder(CollectionViewModel parent)
    {
        var baseName = "New Folder";
        var newPath = Path.Combine(parent.Model.Path, baseName);

        int i = 1;
        while (Directory.Exists(newPath))
        {
            newPath = Path.Combine(parent.Model.Path, $"{baseName} {i++}");
        }

        Directory.CreateDirectory(newPath);

        var subCol = new Collection
        {
            Name = Path.GetFileName(newPath),
            Path = newPath,
            Parent = parent.Model
        };

        return new CollectionViewModel(subCol, _workspaceService);
    }

    public void DeleteItem(ITreeItemViewModel item)
    {
        if (item is CollectionViewModel col)
        {
            if (Directory.Exists(col.Model.Path))
            {
                Directory.Delete(col.Model.Path, true);
            }
        }
        else if (item is RequestItemViewModel req)
        {
            if (Directory.Exists(req.Model.Path))
            {
                Directory.Delete(req.Model.Path, true);
            }
            else if (File.Exists(req.Model.Path))
            {
                File.Delete(req.Model.Path);
            }
        }
    }

    public void RenameItem(ITreeItemViewModel item, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty.");

        if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Name contains invalid characters.");

        if (item is CollectionViewModel col)
        {
            var oldPath = col.Model.Path;
            var parentDir = Path.GetDirectoryName(oldPath);
            if (parentDir == null) return;

            var newPath = Path.Combine(parentDir, newName);

            if (oldPath != newPath && Directory.Exists(newPath))
            {
                newPath = ResolveNameConflict(newPath, newName);
                // Update the name to the resolved one
                newName = Path.GetFileName(newPath);
            }

            if (oldPath != newPath)
            {
                Directory.Move(oldPath, newPath);
                col.Model.Path = newPath;
                col.Model.Name = newName;
            }
        }
        else if (item is RequestItemViewModel req)
        {
            var oldPath = req.Model.Path;
            var parentDir = Path.GetDirectoryName(oldPath);
            if (parentDir == null) return;

            // Preserve extension if it exists, or add .req if it's a bundle
            var extension = Path.GetExtension(oldPath);
            if (string.IsNullOrEmpty(extension) && Directory.Exists(oldPath)) extension = ".req";

            var newPath = Path.Combine(parentDir, newName + extension);

            if (oldPath != newPath && (Directory.Exists(newPath) || File.Exists(newPath)))
            {
                newPath = ResolveNameConflict(newPath, newName + extension);
                newName = Path.GetFileNameWithoutExtension(newPath);
            }

            if (oldPath != newPath)
            {
                if (Directory.Exists(oldPath))
                {
                    Directory.Move(oldPath, newPath);
                }
                else if (File.Exists(oldPath))
                {
                    File.Move(oldPath, newPath);
                }

                req.Model.Path = newPath;
                req.Model.Name = newName;
            }
        }
    }

    public void MoveItem(ITreeItemViewModel item, CollectionViewModel target)
    {
        string? sourcePath = null;
        string? fileName = null;

        if (item is CollectionViewModel draggedCol)
        {
            sourcePath = draggedCol.Model.Path;
            fileName = draggedCol.Name;
        }
        else if (item is RequestItemViewModel draggedReq)
        {
            sourcePath = draggedReq.Model.Path;
            // Use full directory name for bundles
            fileName = Path.GetFileName(sourcePath);
        }

        if (sourcePath == null || fileName == null) return;

        var destPath = Path.Combine(target.Model.Path, fileName);
        bool isDirectory = Directory.Exists(sourcePath);

        if ((isDirectory && Directory.Exists(destPath)) || (!isDirectory && File.Exists(destPath)))
        {
            destPath = ResolveNameConflict(destPath, fileName);
        }

        if (item is CollectionViewModel col)
        {
            Directory.Move(sourcePath, destPath);
            col.Model.Path = destPath;
            col.Model.Name = Path.GetFileName(destPath);
        }
        else if (item is RequestItemViewModel req)
        {
            if (isDirectory)
            {
                Directory.Move(sourcePath, destPath);
            }
            else
            {
                File.Move(sourcePath, destPath);
            }
            req.Model.Path = destPath;
            req.Model.Name = Path.GetFileNameWithoutExtension(destPath);
        }
    }

    public string ResolveNameConflict(string destinationPath, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var directory = Path.GetDirectoryName(destinationPath) ?? "";

        var newPath = destinationPath;
        int counter = 1;

        while (File.Exists(newPath) || Directory.Exists(newPath))
        {
            var newName = $"{nameWithoutExt} ({counter}){extension}";
            newPath = Path.Combine(directory, newName);
            counter++;
        }

        return newPath;
    }

    public bool ValidateFileName(string name, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage = "Name cannot be empty.";
            return false;
        }
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            errorMessage = "Name contains invalid characters.";
            return false;
        }
        errorMessage = null;
        return true;
    }
}
