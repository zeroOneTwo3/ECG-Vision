namespace EcgVision.Core.Domain.Enums;

public enum EcgJobStatus
{
    None = 0,

    // API receives binary, saves to temp disk/S3
    Pending = 1,

    // Worker converts Binary → JSON
    Parsing = 2,

    // Worker pushes JSON to S3, updates Metadata
    JsonUploaded = 3,

    // Generate ECG Plot from JSON
    GeneratingImage = 4,

    // Worker pushes image to S3
    ImageUploaded = 5,

    // Successful completion
    Completed = 6,

    // Some error occured
    Failed = 7,
}