using Gantry.Core.Domain.Collections;
using System.Threading.Tasks;

namespace Gantry.Infrastructure.Export;

public interface ICollectionExporter
{
    string Name { get; }
    Task<byte[]> ExportAsync(Collection collection);
}