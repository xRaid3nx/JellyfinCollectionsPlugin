using Jellyfin.Plugin.Collections.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Collections;

/// <summary>
/// Registers the plugin's services with Jellyfin's DI container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<ILibraryCatalog, LibraryCatalog>();
        serviceCollection.AddSingleton<ICollectionService, CollectionService>();
        serviceCollection.AddSingleton<ICollectionArtwork, CollectionArtwork>();
        serviceCollection.AddSingleton<BatchCollectionsService>();
        serviceCollection.AddSingleton<ISeerrSettings, PluginSeerrSettings>();
        serviceCollection.AddSingleton<SeerrService>();
    }
}
