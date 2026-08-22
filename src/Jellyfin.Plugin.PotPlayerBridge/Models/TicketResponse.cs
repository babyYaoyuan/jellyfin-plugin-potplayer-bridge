namespace Jellyfin.Plugin.PotPlayerBridge.Models;

/// <summary>
/// Response returned to Jellyfin Web after a stream ticket is created.
/// </summary>
/// <param name="StreamPath">Same-origin path for PotPlayer to open.</param>
/// <param name="ExpiresAtUtc">Ticket expiration time.</param>
public sealed record TicketResponse(string StreamPath, DateTimeOffset ExpiresAtUtc);
