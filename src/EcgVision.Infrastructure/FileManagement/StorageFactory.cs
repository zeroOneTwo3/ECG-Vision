using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Interfaces.Services;
using EcgVision.Infrastructure.Configuration;

using Microsoft.Extensions.Options;

namespace EcgVision.Infrastructure.FileManagement;

public class StorageFactory(
    IEnumerable<IStorageService> services,
    IOptions<StorageOptions> options) : IStorageFactory
{
    public IStorageService GetService(DataContext context)
    {
        return context switch
        {
            DataContext.Raw => services.First(s => s.Location == StorageLocation.Local),
            DataContext.Json => services.First(s => s.Location == options.Value.JsonEcgLocation),
            DataContext.Image => services.First(s => s.Location == options.Value.ImageEcgLocation),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
