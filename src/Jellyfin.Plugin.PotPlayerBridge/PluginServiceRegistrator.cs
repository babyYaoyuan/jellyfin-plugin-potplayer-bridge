using Jellyfin.Plugin.PotPlayerBridge.Services;
using Jellyfin.Plugin.PotPlayerBridge.Web;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.PotPlayerBridge;

/// <summary>
/// Registers the plugin's services in Jellyfin's dependency injection container.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services, IServerApplicationHost applicationHost)
    {
        services.AddSingleton(new StreamTicketStore(TimeProvider.System));
        services.AddSingleton<IStartupFilter, IndexHtmlInjectionFilter>();
    }
}
