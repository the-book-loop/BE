using API.Services.Interfaces;
using DB.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController(
    IReviewService reviewService,
    ILogger<ReviewController> logger)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetReviews([FromQuery] ReviewFilterRequest? request = null)
    {
        var result = await reviewService.GetReviewsAsync(request);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve reviews: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} reviews", result.Data.Count());
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetReviewById(Guid id)
    {
        var result = await reviewService.GetReviewByIdAsync(id);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve review {ReviewId}: {Error}", id, result.Error);

            if (result.Error == "Review not found")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved review {ReviewId}", id);
        return Ok(result.Data);
    }

    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<ReviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetUserReviews(Guid userId)
    {
        var result = await reviewService.GetReviewsAsync(new ReviewFilterRequest { ReviewedUserId = userId });

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve reviews for user {UserId}: {Error}", userId, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} reviews for user {UserId}", result.Data.Count(), userId);
        return Ok(result.Data);
    }

    [HttpGet("user/{userId}/rating")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetUserRating(Guid userId)
    {
        var averageResult = await reviewService.GetUserAverageRatingAsync(userId);
        var countResult = await reviewService.GetUserReviewCountAsync(userId);

        if (!averageResult.Success || !countResult.Success)
        {
            logger.LogWarning("Failed to retrieve rating for user {UserId}", userId);
            return BadRequest(new { error = "Failed to retrieve user rating" });
        }

        return Ok(new
        {
            userId,
            averageRating = averageResult.Data,
            totalReviews = countResult.Data
        });
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> AddReview([FromBody] AddReviewRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await reviewService.AddReviewAsync(userId, request);

        if (!result.Success)
        {
            logger.LogWarning("Failed to add review for exchange {ExchangeId} by user {UserId}: {Error}",
                request.ExchangeId, userId, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Review {ReviewId} added successfully by user {UserId}", result.Data.Id, userId);
        return CreatedAtAction(nameof(GetReviewById), new { id = result.Data.Id }, result.Data);
    }

    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateReview(Guid id, [FromBody] UpdateReviewRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await reviewService.UpdateReviewAsync(id, userId, request);

        if (!result.Success)
        {
            logger.LogWarning("Failed to update review {ReviewId} for user {UserId}: {Error}", id, userId, result.Error);

            if (result.Error == "Review not found")
            {
                return NotFound(new { error = result.Error });
            }

            if (result.Error == "You are not authorized to update this review")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Review {ReviewId} updated successfully by user {UserId}", id, userId);
        return Ok(result.Data);
    }

    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteReview(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await reviewService.DeleteReviewAsync(id, userId);

        if (!result.Success)
        {
            logger.LogWarning("Failed to delete review {ReviewId} for user {UserId}: {Error}", id, userId, result.Error);

            if (result.Error == "Review not found")
            {
                return NotFound(new { error = result.Error });
            }

            if (result.Error == "You are not authorized to delete this review")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Review {ReviewId} deleted successfully by user {UserId}", id, userId);
        return NoContent();
    }
}
