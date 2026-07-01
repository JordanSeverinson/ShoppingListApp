using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingList.Api.Contracts;
using ShoppingList.Api.Services;

namespace ShoppingList.Api.Controllers;

[ApiController]
[Route("api/friends")]
[Authorize]
public class FriendsController(
    CurrentUserService currentUser,
    FriendsService friendsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(FriendsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FriendsResponse>> GetFriends(CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        return Ok(await friendsService.GetFriendsAsync(userId, cancellationToken));
    }

    [HttpPost("requests")]
    [ProducesResponseType(typeof(SendFriendRequestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SendFriendRequestResponse>> SendFriendRequest(
        [FromBody] SendFriendRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        try
        {
            return Ok(await friendsService.SendRequestAsync(userId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("requests/{requestId:guid}/accept")]
    [ProducesResponseType(typeof(FriendSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FriendSummaryDto>> AcceptFriendRequest(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        try
        {
            return Ok(await friendsService.AcceptRequestAsync(userId, requestId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("requests/{requestId:guid}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeclineFriendRequest(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        try
        {
            await friendsService.DeclineRequestAsync(userId, requestId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{friendUserId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFriend(Guid friendUserId, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        try
        {
            await friendsService.RemoveFriendAsync(userId, friendUserId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
