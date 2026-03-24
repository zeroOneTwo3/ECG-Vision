using EcgVision.Core.Dtos;

namespace EcgVision.Core.Interfaces;

public interface IRawEcgFilesManager
{
    Task<string?> GetRecordNameAsync(EcgFilesDto ecgFilesDto);
    Guid CreateGuidFromRecordName(string recordName);
    Task<string> SaveAsync(EcgFilesDto ecgFilesDto, string filesDirectory);
    Task<(Guid? JobId, string? ErrorMessage)> QueueEcgJobAsync(EcgUploadDto ecgUploadDto, Guid userId);
}
