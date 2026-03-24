using System.Text.Json;
using System.Text.Json.Serialization;

using CliWrap;
using CliWrap.Buffered;

using EcgVision.Core.Dtos;
using EcgVision.Core.Interfaces;
using EcgVision.Infrastructure.Configuration;

using Microsoft.Extensions.Options;

namespace EcgVision.Infrastructure.FileManagement;

public class CliEcgSignalParser : IEcgSignalParser
{

    private readonly string _pythonExe;
    private readonly string _scriptPath;
    private const string ScriptName = "extract_leads.py";

    public CliEcgSignalParser(IOptions<PythonOptions> options)
    {
        var config = options.Value;
        _pythonExe = config.PythonExe;
        _scriptPath = Path.Combine(config.ScriptFolder, ScriptName);
    }
    public async Task<EcgLeadsDto> ExtractLeadsAsync(string recordPath, int leadCount = 4)
    {
        // 1. Execute the Python script
        var result = await Cli.Wrap(_pythonExe)
            .WithArguments(new[] { _scriptPath, recordPath, leadCount.ToString() })
            .ExecuteBufferedAsync();

        if (!string.IsNullOrEmpty(result.StandardError))
        {
            throw new Exception($"Python Error: {result.StandardError}");
        }

        // The Python output is a JSON string: {"LeadII": [...], "LeadV2": [...], ...}
        var leads = JsonSerializer.Deserialize<EcgLeadsDto>(result.StandardOutput, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        });

        return leads ?? throw new Exception("Failed to parse signal data.");
    }
}