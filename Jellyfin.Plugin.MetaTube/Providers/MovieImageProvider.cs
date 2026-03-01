using Jellyfin.Plugin.MetaTube.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
#if __EMBY__
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;

#else
using Microsoft.Extensions.Logging;
#endif

namespace Jellyfin.Plugin.MetaTube.Providers;

public class MovieImageProvider : BaseProvider, IRemoteImageProvider, IHasOrder
{
#if __EMBY__
    public MovieImageProvider(ILogManager logManager) : base(logManager.CreateLogger<MovieImageProvider>())
#else
    public MovieImageProvider(ILogger<MovieImageProvider> logger) : base(logger)
#endif
    {
    }

#if __EMBY__
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
#else
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
#endif
    {
        var pid = item.GetPid(Plugin.ProviderId);
        if (string.IsNullOrWhiteSpace(pid.Id) || string.IsNullOrWhiteSpace(pid.Provider))
            return Enumerable.Empty<RemoteImageInfo>();

        try
        {
            var m = await ApiClient.GetMovieInfoAsync(pid.Provider, pid.Id, cancellationToken);
            var images = new List<RemoteImageInfo>
            {
                new()
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = ApiClient.GetPrimaryImageApiUrl(m.Provider, m.Id, pid.Position ?? -1)
                },
                new()
                {
                    ProviderName = Name,
                    Type = ImageType.Thumb,
                    Url = ApiClient.GetThumbImageApiUrl(m.Provider, m.Id)
                },
                new()
                {
                    ProviderName = Name,
                    Type = ImageType.Backdrop,
                    Url = ApiClient.GetBackdropImageApiUrl(m.Provider, m.Id)
                }
            };

            foreach (var imageUrl in m.PreviewImages ?? Enumerable.Empty<string>())
            {
                images.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = ApiClient.GetPrimaryImageApiUrl(m.Provider, m.Id, imageUrl, pid.Position ?? -1)
                });

                images.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Thumb,
                    Url = ApiClient.GetThumbImageApiUrl(m.Provider, m.Id, imageUrl)
                });

                images.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Backdrop,
                    Url = ApiClient.GetBackdropImageApiUrl(m.Provider, m.Id, imageUrl)
                });
            }

            return images;
        }
        catch (Exception e)
        {
            Logger.Warn("Movie image info lookup failed for {0}. Fallback to direct image URLs. Error: {1}", pid.ToString(), e.Message);

            return new List<RemoteImageInfo>
            {
                new()
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = ApiClient.GetPrimaryImageApiUrl(pid.Provider, pid.Id, pid.Position ?? -1)
                },
                new()
                {
                    ProviderName = Name,
                    Type = ImageType.Thumb,
                    Url = ApiClient.GetThumbImageApiUrl(pid.Provider, pid.Id)
                },
                new()
                {
                    ProviderName = Name,
                    Type = ImageType.Backdrop,
                    Url = ApiClient.GetBackdropImageApiUrl(pid.Provider, pid.Id)
                }
            };
        }
    }

    public bool Supports(BaseItem item)
    {
        return item is Movie;
    }

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new List<ImageType>
        {
            ImageType.Primary,
            ImageType.Thumb,
            ImageType.Backdrop
        };
    }
}