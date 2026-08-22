using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.PotPlayerBridge.Api;

/// <summary>
/// Serves the embedded Jellyfin Web integration script.
/// </summary>
[ApiController]
[Route("Plugins/PotPlayerBridge")]
public sealed class AssetsController : ControllerBase
{
    /// <summary>
    /// Returns the browser script injected into Jellyfin Web.
    /// </summary>
    [HttpGet("client.js")]
    [AllowAnonymous]
    [Produces("application/javascript")]
    public ActionResult GetClientScript()
    {
        var assembly = typeof(AssetsController).Assembly;
        var resourceName = $"{typeof(Plugin).Namespace}.Web.potplayer-bridge.js";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return NotFound("Embedded client script was not found.");
        }

        using var reader = new StreamReader(stream);
        var script = reader.ReadToEnd();
        Response.Headers.CacheControl = "public, max-age=300";
        return Content(script, "application/javascript");
    }
}
