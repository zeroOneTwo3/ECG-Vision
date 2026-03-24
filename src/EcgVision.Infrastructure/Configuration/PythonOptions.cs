using System.ComponentModel.DataAnnotations;

namespace EcgVision.Infrastructure.Configuration;

public class PythonOptions
{
    public const string SectionName = "Python";

    [Required(ErrorMessage = "Python executable path is required.")]
    public string PythonExe { get; set; } = string.Empty;


    [Required(ErrorMessage = "Script folder path is required.")]
    public string ScriptFolder { get; set; } = string.Empty;
}
