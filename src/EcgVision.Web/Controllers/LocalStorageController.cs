using EcgVision.Infrastructure.FileManagement;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcgVision.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocalStorageController(LocalStorageService localStorage) : ControllerBase
{
    [HttpGet("local/{*path}")]
    public async Task<IActionResult> GetLocalFile(string path)
    {
        try
        {
            var stream = await localStorage.GetAsync(path);

            var contentType = GetContentType(path);

            return File(stream, contentType, enableRangeProcessing: true);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    private string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".json" => "application/json",
            ".hea" => "text/plain",
            ".dat" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }
}
