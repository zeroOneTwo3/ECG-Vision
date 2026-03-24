namespace EcgVision.Core.Dtos;

public class EcgFilesDto
{
    public string HeaFileName { get; init; } = string.Empty;

    public string DatFileName { get; init; } = string.Empty;

    /// <summary>
    /// Consumers are responsible for disposing!!!
    /// </summary>
    public Stream HeaFileStream { get; init; } = default!;

    /// <summary>
    /// Consumers are responsible for disposing!!!
    /// </summary>
    public Stream DatFileStream { get; init; } = default!;
}
