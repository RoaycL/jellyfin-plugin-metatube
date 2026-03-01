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

        var candidates = BuildCandidates(pid).ToList();

        // 1) Try movie info endpoints in parallel; use first successful one.
        var infoTasks = candidates.Select(async c =>
        {
            try
            {
                var m = await ApiClient.GetMovieInfoAsync(c.Provider, c.Id, cancellationToken);
                return (ok: true, provider: c.Provider, id: c.Id, movie: m, err: string.Empty);
            }
            catch (Exception e)
            {
                return (ok: false, provider: c.Provider, id: c.Id, movie: default(Jellyfin.Plugin.MetaTube.Metadata.MovieInfo), err: e.Message);
            }
        }).ToList();

        var infoResults = await Task.WhenAll(infoTasks);
        var firstOk = infoResults.FirstOrDefault(x => x.ok);
        if (firstOk.ok && firstOk.movie != null)
        {
            return BuildImages(firstOk.movie.Provider, firstOk.movie.Id, pid.Position, firstOk.movie.PreviewImages);
        }

        Logger.Warn("All movie info lookups failed for {0}. Trying direct image fallbacks.", pid.ToString());
        foreach (var r in infoResults)
        {
            Logger.Warn("Movie info failed: {0}:{1} => {2}", r.provider, r.id, r.err);
        }

        // 2) Direct image fallback: probe candidates, use the first primary image that returns 2xx.
        foreach (var c in candidates)
        {
            var primaryUrl = ApiClient.GetPrimaryImageApiUrl(c.Provider, c.Id, pid.Position ?? -1);
            if (await ProbeImageUrl(primaryUrl, cancellationToken))
            {
                Logger.Info("Movie image fallback matched: {0}:{1}", c.Provider, c.Id);
                return BuildImages(c.Provider, c.Id, pid.Position, Enumerable.Empty<string>());
            }
        }

        // 3) Last resort: return all direct candidates so Emby UI can still try.
        return candidates
            .SelectMany(c => BuildImages(c.Provider, c.Id, pid.Position, Enumerable.Empty<string>()))
            .ToList();
    }

    private IEnumerable<(string Provider, string Id)> BuildCandidates(Jellyfin.Plugin.MetaTube.Helpers.ProviderId pid)
    {
        var list = new List<(string Provider, string Id)>();

        void Add(string provider, string id)
        {
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(id)) return;
            if (list.Any(x => x.Provider == provider && x.Id == id)) return;
            list.Add((provider, id));
        }

        Add(pid.Provider, pid.Id);

        var normalizedId = NormalizeMovieId(pid.Id);
        if (!string.Equals(normalizedId, pid.Id, StringComparison.OrdinalIgnoreCase))
            Add(pid.Provider, normalizedId);

        // Common recovery path: AVBASE ids often cannot fetch images directly, while JavBus can.
        if (string.Equals(pid.Provider, "AVBASE", StringComparison.OrdinalIgnoreCase))
        {
            Add("JavBus", normalizedId);
            Add("JavBus", pid.Id);
            Add("JavDB", normalizedId);
            Add("JavLibrary", normalizedId);
        }

        // Generic fallback routes.
        Add("JavBus", normalizedId);
        Add("JavDB", normalizedId);
        Add("JavLibrary", normalizedId);

        return list;
    }

    private static string NormalizeMovieId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return id;

        var s = id.Trim();

        // Case 1: idp:IPX-337 -> IPX-337
        if (s.StartsWith("idp:", StringComparison.OrdinalIgnoreCase))
            s = s.Substring(4);

        // Case 2: madonna:JUQ-945 / moodyz:MIAA-625 -> JUQ-945 / MIAA-625
        var idx = s.LastIndexOf(':');
        if (idx >= 0 && idx + 1 < s.Length)
            s = s.Substring(idx + 1);

        return s.Trim();
    }

    private IEnumerable<RemoteImageInfo> BuildImages(string provider, string id, double? position, IEnumerable<string> previewImages)
    {
        var pos = position ?? -1;
        var images = new List<RemoteImageInfo>
        {
            new()
            {
                ProviderName = Name,
                Type = ImageType.Primary,
                Url = ApiClient.GetPrimaryImageApiUrl(provider, id, pos)
            },
            new()
            {
                ProviderName = Name,
                Type = ImageType.Thumb,
                Url = ApiClient.GetThumbImageApiUrl(provider, id)
            },
            new()
            {
                ProviderName = Name,
                Type = ImageType.Backdrop,
                Url = ApiClient.GetBackdropImageApiUrl(provider, id)
            }
        };

        foreach (var imageUrl in previewImages ?? Enumerable.Empty<string>())
        {
            images.Add(new RemoteImageInfo
            {
                ProviderName = Name,
                Type = ImageType.Primary,
                Url = ApiClient.GetPrimaryImageApiUrl(provider, id, imageUrl, pos)
            });

            images.Add(new RemoteImageInfo
            {
                ProviderName = Name,
                Type = ImageType.Thumb,
                Url = ApiClient.GetThumbImageApiUrl(provider, id, imageUrl)
            });

            images.Add(new RemoteImageInfo
            {
                ProviderName = Name,
                Type = ImageType.Backdrop,
                Url = ApiClient.GetBackdropImageApiUrl(provider, id, imageUrl)
            });
        }

        return images;
    }

    private async Task<bool> ProbeImageUrl(string url, CancellationToken cancellationToken)
    {
        try
        {
            var resp = await ApiClient.GetImageResponse(url, cancellationToken);
#if __EMBY__
            try { resp.Content?.Dispose(); } catch { }
            var code = (int)resp.StatusCode;
            return code >= 200 && code < 300;
#else
            return resp.IsSuccessStatusCode;
#endif
        }
        catch
        {
            return false;
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
