using System;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;

namespace Jellyfin.Server.Implementations.Events.Consumers.Library;

/// <summary>
/// Creates an entry in the activity log whenever a lyric download fails.
/// </summary>
public class LyricDownloadFailureLogger : IEventConsumer<LyricDownloadFailureEventArgs>
{
    private readonly ILocalizationManager _localizationManager;
    private readonly IActivityManager _activityManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LyricDownloadFailureLogger"/> class.
    /// </summary>
    /// <param name="localizationManager">The localization manager.</param>
    /// <param name="activityManager">The activity manager.</param>
    public LyricDownloadFailureLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
    {
        _localizationManager = localizationManager;
        _activityManager = activityManager;
    }

    /// <inheritdoc />
    public async Task OnEvent(LyricDownloadFailureEventArgs eventArgs)
    {
        await _activityManager.CreateAsync(new ActivityLog(
            string.Format(
                CultureInfo.InvariantCulture,
                _localizationManager.GetLocalizedString("LyricDownloadFailureFromForItem"),
                eventArgs.Provider,
                GetItemName(eventArgs.Item)),
            "LyricDownloadFailure",
            Guid.Empty)
        {
            ItemId = eventArgs.Item.Id.ToString("N", CultureInfo.InvariantCulture),
            ShortOverview = eventArgs.Exception.Message
        }).ConfigureAwait(false);
    }

    private static string GetItemName(BaseItem item)
    {
        var name = item.Name;
        if (item is Episode episode)
        {
            if (episode.IndexNumber.HasValue)
            {
                name = string.Format(
                    CultureInfo.InvariantCulture,
                    "Ep{0} - {1}",
                    episode.IndexNumber.Value,
                    name);
            }

            if (episode.ParentIndexNumber.HasValue)
            {
                name = string.Format(
                    CultureInfo.InvariantCulture,
                    "S{0}, {1}",
                    episode.ParentIndexNumber.Value,
                    name);
            }
        }

        if (item is IHasSeries hasSeries)
        {
            name = hasSeries.SeriesName + " - " + name;
        }

        if (item is IHasAlbumArtist hasAlbumArtist)
        {
            var artists = hasAlbumArtist.AlbumArtists;

            if (artists.Count > 0)
            {
                name = artists[0] + " - " + name;
            }
        }
        else if (item is IHasArtist hasArtist)
        {
            var artists = hasArtist.Artists;

            if (artists.Count > 0)
            {
                name = artists[0] + " - " + name;
            }
        }

        return name;
    }
}
