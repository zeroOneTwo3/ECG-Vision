using EcgVision.Core.Dtos;
using EcgVision.Core.Interfaces.Services;

using Microsoft.AspNetCore.Mvc;

namespace EcgVision.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IIdentityService identityService) : ControllerBase
{
    //[Authorize(Roles = "Administrator")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        var result = await identityService.RegisterUserAsync(model);

        if (result.Succeeded)
        {
            return Ok(new { Message = $"User registered successfully as {model.Role}" });
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginDto)
    {
        var response = await identityService.AuthenticateAsync(loginDto.Email, loginDto.Password);

        if (response == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        return Ok(response);
    }
}