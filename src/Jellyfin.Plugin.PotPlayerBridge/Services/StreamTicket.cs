namespace Jellyfin.Plugin.PotPlayerBridge.Services;

/// <summary>
/// A random bearer ticket restricted to one Jellyfin library file.
/// </summary>
public sealed record StreamTicket(
    string Token,
    Guid ItemId,
    string Path,
    string FileName,
    string ContentType,
    DateTimeOffset ExpiresAtUtc,
    string? RemoteAddress);
