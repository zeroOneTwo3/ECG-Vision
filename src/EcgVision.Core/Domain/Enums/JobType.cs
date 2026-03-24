namespace EcgVision.Core.Domain.Enums;

public enum JobType
{
    // Technical Extraction Tasks
    Extract4Leads = 10, // The 4-Lead (Rhythm Focus)
    Extract12Leads = 20, // The 12-Lead (Morphology Focus)

    // Clinical Diagnostic Tasks
    RhythmAnalysis = 30,      // Common for 4-lead (Arrhythmia detection)
    FullDiagnostic12L = 40,   // Common for 12-lead (Infarction/Ischemia)

    // Preventive/Screening
    BaselineScreening = 50,
    UrgentTriage = 60
}