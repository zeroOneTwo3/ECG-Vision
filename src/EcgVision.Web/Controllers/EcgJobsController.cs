using EcgVision.Core.Constants;
using EcgVision.Core.Interfaces.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EcgJobsController(
    IEcgJobService ecgJobService,
    IAuthorizationService authorizationService,
    ILogger<EcgJobsController> logger) : ControllerBase
{
    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> GetStatus(Guid jobId)
    {
        var job = await ecgJobService.GetByIdWithSignalAsync(jobId);
        if (job == null)
            return NotFound();

        var authorizationResult = await authorizationService.AuthorizeAsync(User, job, AppConstants.ViewJobPolicy);
        if (!authorizationResult.Succeeded)
            return NotFound();

        return Ok(new
        {
            JobId = job.Id,
            Status = job.Status.ToString(),
            Progress = job.ProgressPercentage
        });
    }

    [HttpGet("download/{jobId}")]
    public async Task<IActionResult> GetDownloadLink(Guid jobId)
    {
        var job = await ecgJobService.GetByIdWithSignalAsync(jobId);
        if (job == null)
            return NotFound();

        var authorizationResult = await authorizationService.AuthorizeAsync(User, job, AppConstants.ViewJobPolicy);
        if (!authorizationResult.Succeeded)
            return NotFound();

        var result = await ecgJobService.GetPlotPresignedUrlAsync(job);
        if (result.Url == null || !string.IsNullOrWhiteSpace(result.Error))
            return NotFound();

        logger.LogInformation("Presigned URL generated for Job: {JobId} by User: {User}", jobId, User.Identity?.Name);

        return Ok(new
        {
            result.Url,
            result.ExpiresAt
        });
    }
}