using System.Text.Json.Serialization;

namespace EcgVision.Core.Dtos;

public class EcgLeadsDto
{
    public int SamplingRate { get; set; }
    public int LeadCount { get; set; }

    // --- The 12 Standard Leads ---
    public double[] I { get; set; } = [];
    public double[] II { get; set; } = [];
    public double[] III { get; set; } = [];
    public double[] AVR { get; set; } = [];
    public double[] AVL { get; set; } = [];
    public double[] AVF { get; set; } = [];
    public double[] V1 { get; set; } = [];
    public double[] V2 { get; set; } = [];
    public double[] V3 { get; set; } = [];
    public double[] V4 { get; set; } = [];
    public double[] V5 { get; set; } = [];
    public double[] V6 { get; set; } = [];

    // --- 2. The Screening Subset (Convenience Properties) ---
    // These point directly to the data above, so no extra memory is used.
    [JsonIgnore]
    public double[] LeadII => II;
    [JsonIgnore]
    public double[] LeadV2 => V2;
    [JsonIgnore]
    public double[] LeadV4 => V4;
    [JsonIgnore]
    public double[] LeadV6 => V6;

    // --- 3. The Enumerable Properties ---

    /// <summary>
    /// Returns only the 4 leads used for rapid screening.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<(string Label, double[] Data)> FourLeads => new[]
    {
        ("Lead II", II),
        ("Lead V2", V2),
        ("Lead V4", V4),
        ("Lead V6", V6)
    };

    /// <summary>
    /// Returns all 12 leads for full diagnostic plotting/analysis.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<(string Label, double[] Data)> AllLeads => new[]
    {
        ("I", I), ("II", II), ("III", III),
        ("aVR", AVR), ("aVL", AVL), ("aVF", AVF),
        ("V1", V1), ("V2", V2), ("V3", V3),
        ("V4", V4), ("V5", V5), ("V6", V6)
    };
}