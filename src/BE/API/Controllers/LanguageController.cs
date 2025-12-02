using API.Services.Interfaces;
using DB.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LanguageController(
    ILanguageService languageService,
    ILogger<LanguageController> logger)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LanguageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetLanguagesAsync()
    {
        var result = await languageService.GetLanguagesAsync();

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve languages: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} languages", result.Data.Count());
        return Ok(result.Data);
    }
}
