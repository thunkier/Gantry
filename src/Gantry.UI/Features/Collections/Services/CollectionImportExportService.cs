using System.Threading.Tasks;
using Gantry.Core.Domain.Collections;
using Gantry.Infrastructure.Export;
using Gantry.Infrastructure.Persistence;

namespace Gantry.UI.Features.Collections.Services;

public class CollectionImportExportService
{
    public async Task ExportAsync(Collection collection, string filePath)
    {
        ICollectionExporter? exporter = GetExporterForPath(filePath);

        if (exporter != null)
        {
            // Now works for both Strings (UTF8 bytes) and Zips (Binary bytes)
            byte[] content = await exporter.ExportAsync(collection);
            await File.WriteAllBytesAsync(filePath, content);
        }
    }

    private ICollectionExporter? GetExporterForPath(string filePath)
    {
        // Simple factory logic
        if (filePath.EndsWith(".tsp")) return new TypeSpecExporter();

        // Note: The Bruno exporter now produces a ZIP, so we should probably 
        // check for .zip, or just know that .bru in this context means "Bruno Archive"
        if (filePath.EndsWith(".bru") || filePath.EndsWith(".zip")) return new BrunoExporter();

        if (filePath.EndsWith(".json")) return new OpenApiExporter();

        return null;
    }

    public async Task<Collection> ImportAsync(string filePath, string type)
    {
        if (type == "Postman Collection v2.1")
        {
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            var parser = new PostmanCollectionParser();
            var collection = parser.Parse(json);
            collection.Path = filePath;
            return collection;
        }
        else // Gantry JSON
        {
            var repo = new FileSystemCollectionRepository();
            return repo.LoadCollection(filePath);
        }
    }
}
