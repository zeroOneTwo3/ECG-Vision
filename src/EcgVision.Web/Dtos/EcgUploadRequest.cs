using System.ComponentModel.DataAnnotations;

using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Dtos;

namespace EcgVision.Web.Dtos;

public class EcgUploadRequest
{
    [Required]
    public Guid PatientId { get; init; }

    [Required]
    public JobType ProcessingType { get; init; } = JobType.Extract4Leads;

    [Required]
    public List<IFormFile> Files { get; init; } = [];

    // Validation Constants
    /// <summary>
    /// The .hea (Header) file is a text file that contains the "instructions"
    /// (metadata like sampling frequency, the number of leads).
    /// In WFDB format, the .hea file is usually just a few kilobytes of text.
    /// </summary>
    private const long MaxHeaSize = 100 * 1024;

    /// <summary>
    /// The .dat (Data) file is a binary file that contains the actual voltage signals 
    /// (the raw ECG data) and can range from a few hundred KB to several MBs.
    /// </summary>
    private const long MaxDatSize = 20 * 1024 * 1024;

    public (bool IsValid, string ErrorMessage) ValidateFiles()
    {
        if (Files.Count != 2)
            return (false, "Please upload exactly two files (.hea and .dat).");

        var heaFile = Files.FirstOrDefault(f => f.FileName.EndsWith(".hea", StringComparison.OrdinalIgnoreCase));
        var datFile = Files.FirstOrDefault(f => f.FileName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase));

        if (heaFile == null || datFile == null)
            return (false, "Missing required file type. One .hea and one .dat file are required.");

        if (heaFile.Length > MaxHeaSize)
            return (false, $"The .hea file is too large (Max {MaxHeaSize / 1024}KB).");

        if (datFile.Length > MaxDatSize)
            return (false, $"The .dat file is too large (Max {MaxDatSize / 1024 / 1024}MB).");

        if (Path.GetFileNameWithoutExtension(heaFile.FileName) != Path.GetFileNameWithoutExtension(datFile.FileName))
            return (false, "Filenames for .hea and .dat must be identical.");

        return (true, string.Empty);
    }

    public EcgFilesDto GetEcgFilesDto()
    {
        var heaFile = Files.First(f => f.FileName.EndsWith(".hea", StringComparison.OrdinalIgnoreCase));
        var datFile = Files.First(f => f.FileName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase));

        return new EcgFilesDto()
        {
            HeaFileName = heaFile.FileName,
            DatFileName = datFile.FileName,
            HeaFileStream = heaFile.OpenReadStream(),
            DatFileStream = datFile.OpenReadStream()
        };
    }
}