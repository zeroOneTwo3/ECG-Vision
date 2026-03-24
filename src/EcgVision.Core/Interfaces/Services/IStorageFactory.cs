using EcgVision.Core.Domain.Enums;

namespace EcgVision.Core.Interfaces.Services;

public interface IStorageFactory
{
    IStorageService GetService(DataContext context);
}
