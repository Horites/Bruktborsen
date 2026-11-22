
using Bruktborsen.Api.DTOs.Auth;
using Bruktborsen.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            return BadRequest(new
            {
                message = "FullName is required."
            });
        }

        var email = dto.Email.Trim();

        var existingUser = await _userManager.FindByEmailAsync(
            email);

        if (existingUser is not null)
        {
            return BadRequest(new
            {
                message = "Email is already registered."
            });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = dto.FullName.Trim()
        };

        var result = await _userManager.CreateAsync(
            user,
            dto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(e => new
                {
                    e.Code,
                    e.Description
                })
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "User");

        if (!roleResult.Succeeded)
        {
            // Avoid leaving an account without the required role.
            await _userManager.DeleteAsync(user);

            return Problem(
                title: "Registration failed",
                detail: "Unable to assign the default role.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                user.Id,
                user.Email,
                user.FullName,
                Role = "User"
            });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(
        typeof(CurrentUserDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> GetMe()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new CurrentUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            roles
        ));
    }
}
