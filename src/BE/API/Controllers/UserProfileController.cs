using API.Services.Interfaces;
using DB.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProfileController(
    IUserProfileService userProfileService,
    ILogger<UserProfileController> logger)
    : ControllerBase
{
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(UserPublicProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetUserProfile(Guid userId)
    {
        Guid? requesterId = null;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedRequesterId))
        {
            requesterId = parsedRequesterId;
        }

        var result = await userProfileService.GetUserPublicProfileAsync(userId, requesterId);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve user profile {UserId}: {Error}", userId, result.Error);

            if (result.Error == "User not found")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved user profile {UserId}", userId);
        return Ok(result.Data);
    }

    [HttpGet("exchangeable")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<UserPublicProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetExchangeableUsers()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await userProfileService.GetExchangeableUsersAsync(userId);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve exchangeable users for {UserId}: {Error}", userId, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} exchangeable users for user {UserId}", result.Data.Count(), userId);
        return Ok(result.Data);
    }
}
