using System.Security.Claims;

using EcgVision.Core.Dtos;
using EcgVision.Core.Interfaces;
using EcgVision.Core.Interfaces.Services;
using EcgVision.Web.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcgVision.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator,Doctor")]
public class EcgIngestionController : ControllerBase
{
    private readonly IRawEcgFilesManager _rawEcgFileManager;
    private readonly IPatientService _patientService;
    private readonly ILogger<EcgIngestionController> _logger;

    public EcgIngestionController(
        IRawEcgFilesManager rawEcgFileManager,
        IPatientService patientService,
        ILogger<EcgIngestionController> logger)
    {
        _logger = logger;
        _rawEcgFileManager = rawEcgFileManager;
        _patientService = patientService;
    }


    [HttpPost("upload-ptbxl")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPtbxl([FromForm] EcgUploadRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity is invalid.");

        if (request.PatientId == Guid.Empty)
            return BadRequest("Invalid Patient ID.");

        if (!(await _patientService.ExistsAsync(request.PatientId)))
            return BadRequest("Patient record not found.");

        var validatedFilesResult = request.ValidateFiles();
        if (!validatedFilesResult.IsValid)
            return BadRequest(validatedFilesResult.ErrorMessage);

        (Guid? jobId, string? error) = await _rawEcgFileManager.QueueEcgJobAsync(new EcgUploadDto
        {
            UserId = userId,
            PatientId = request.PatientId,
            JobType = request.ProcessingType,
            Files = request.GetEcgFilesDto()
        }, userId);

        if (jobId == null)
        {
            _logger.LogError("Upload failed for patient {PatientId}: {Error}", request.PatientId, error);
            return StatusCode(500, "An error occurred while queuing the ECG signal.");
        }

        return AcceptedAtAction(
            actionName: "GetStatus",
            controllerName: "EcgJobs",
            routeValues: new { jobId = jobId },
            value: new { JobId = jobId, Message = "ECG signal accepted and queued." });
    }
}