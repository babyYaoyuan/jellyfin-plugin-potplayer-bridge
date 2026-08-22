using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PotPlayerBridge.Configuration;

/// <summary>
/// User-configurable plugin settings.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the lifetime of a media stream ticket.
    /// </summary>
    public int TicketLifetimeMinutes { get; set; } = 720;

    /// <summary>
    /// Gets or sets a value indicating whether a ticket is restricted to the
    /// network address that created it.
    /// </summary>
    public bool BindTicketToRemoteAddress { get; set; } = true;
}
