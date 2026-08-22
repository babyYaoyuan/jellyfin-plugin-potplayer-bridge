using Jellyfin.Plugin.PotPlayerBridge.Models;
using Jellyfin.Plugin.PotPlayerBridge.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PotPlayerBridge.Api;

/// <summary>
/// Creates scoped stream tickets and serves original library files to PotPlayer.
/// </summary>
[ApiController]
[Route("Plugins/PotPlayerBridge")]
public sealed class PlaybackController : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
    private readonly ILibraryManager _libraryManager;
    private readonly StreamTicketStore _ticketStore;
    private readonly ILogger<PlaybackController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackController"/> class.
    /// </summary>
    public PlaybackController(
        ILibraryManager libraryManager,
        StreamTicketStore ticketStore,
        ILogger<PlaybackController> logger)
    {
        _libraryManager = libraryManager;
        _ticketStore = ticketStore;
        _logger = logger;
    }

    /// <summary>
    /// Creates a temporary stream ticket for an authenticated user's library item.
    /// </summary>
    [HttpPost("Tickets/{itemId:guid}")]
    [Authorize]
    public ActionResult<TicketResponse> CreateTicket([FromRoute] Guid itemId)
    {
        var userIdClaim = User.Claims.FirstOrDefault(
            claim => claim.Type.Equals("Jellyfin-UserId", StringComparison.OrdinalIgnoreCase));
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Forbid();
        }

        // Use Jellyfin's user-aware lookup so parental controls, library access,
        // and item visibility are enforced before a ticket is issued.
        var item = _libraryManager.GetItemById<BaseItem>(itemId, userId);
        if (item is null)
        {
            return NotFound();
        }

        var path = item.Path;
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
        {
            return BadRequest(new { Error = "The item is not backed by a readable local file." });
        }

        if (!ContentTypeProvider.TryGetContentType(path, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var config = Plugin.Instance?.Configuration;
        var lifetimeMinutes = Math.Clamp(config?.TicketLifetimeMinutes ?? 720, 5, 1440);
        var remoteAddress = config?.BindTicketToRemoteAddress == false
            ? null
            : HttpContext.Connection.RemoteIpAddress;
        var ticket = _ticketStore.Create(
            itemId,
            path,
            contentType,
            TimeSpan.FromMinutes(lifetimeMinutes),
            remoteAddress);

        var encodedFileName = Uri.EscapeDataString(ticket.FileName);
        var streamPath = $"{Request.PathBase}/Plugins/PotPlayerBridge/Stream/{ticket.Token}/{encodedFileName}";
        _logger.LogInformation(
            "Created PotPlayer stream ticket for item {ItemId}; expires at {ExpiresAt}",
            itemId,
            ticket.ExpiresAtUtc);
        return Ok(new TicketResponse(streamPath, ticket.ExpiresAtUtc));
    }

    /// <summary>
    /// Serves the ticket's original file with byte range support.
    /// </summary>
    [AcceptVerbs("GET", "HEAD")]
    [Route("Stream/{token}/{fileName}")]
    [AllowAnonymous]
    public ActionResult StreamOriginal([FromRoute] string token, [FromRoute] string fileName)
    {
        _ = fileName;
        if (!_ticketStore.TryGet(token, HttpContext.Connection.RemoteIpAddress, out var ticket) || ticket is null)
        {
            return NotFound();
        }

        if (!System.IO.File.Exists(ticket.Path))
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.Pragma = "no-cache";
        return PhysicalFile(ticket.Path, ticket.ContentType, enableRangeProcessing: true);
    }
}
