using System;
using System.IO;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Providers.Plugins.NfoCreateDate.Configuration;
using Xunit;

namespace Jellyfin.Plugin.DateAddedAdvanced.Tests;

[Collection("PluginTests")]
public class PathResolverTests
{
    [Fact]
    public void GetXmlRootNodeName_ReturnsCorrectRootForEachItemType()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            UseSeasonDateForEpisodes = false
        });

        Assert.Equal("movie", PathResolver.GetXmlRootNodeName(new Movie()));
        Assert.Equal("tvshow", PathResolver.GetXmlRootNodeName(new Series()));
        Assert.Equal("season", PathResolver.GetXmlRootNodeName(new Season()));
        Assert.Equal("episodedetails", PathResolver.GetXmlRootNodeName(new Episode()));
        Assert.Equal("album", PathResolver.GetXmlRootNodeName(new MusicAlbum()));
        Assert.Equal("artist", PathResolver.GetXmlRootNodeName(new MusicArtist()));
        Assert.Equal("album", PathResolver.GetXmlRootNodeName(new Audio()));
    }

    [Fact]
    public void GetXmlRootNodeName_ForEpisode_WhenUseSeasonDateForEpisodesIsTrue_DistinguishesReadAndWriteMode()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            UseSeasonDateForEpisodes = true
        });

        // In read mode, it reads season.nfo which has <season> root
        Assert.Equal("season", PathResolver.GetXmlRootNodeName(new Episode(), modeRead: true));

        // In write mode, it writes [episode].nfo which must have <episodedetails> root
        Assert.Equal("episodedetails", PathResolver.GetXmlRootNodeName(new Episode(), modeRead: false));
    }

    [Fact]
    public void GetXmlPathInfoForItem_ForMovieInDedicatedFolder_ReturnsMovieNfo()
    {
        using var helper = new TestPluginHelper();

        var folder = Path.Combine(helper.TempDir, "Gladiator (2000)");
        Directory.CreateDirectory(folder);
        var moviePath = Path.Combine(folder, "gladiator.mkv");
        File.WriteAllText(moviePath, "video");

        var movie = new Movie
        {
            Path = moviePath,
            IsInMixedFolder = false
        };

        var nfoPath = PathResolver.GetXmlPathInfoForItem(movie, modeRead: true);
        Assert.Equal(Path.Combine(folder, "movie.nfo"), nfoPath);
    }

    [Fact]
    public void GetXmlPathInfoForItem_ForSeriesAndSeason_ReturnsTvshowNfoAndSeasonNfo()
    {
        using var helper = new TestPluginHelper();

        var seriesFolder = Path.Combine(helper.TempDir, "Breaking Bad");
        Directory.CreateDirectory(seriesFolder);
        var seasonFolder = Path.Combine(seriesFolder, "Season 01");
        Directory.CreateDirectory(seasonFolder);

        var series = new Series { Path = seriesFolder };
        var season = new Season { Path = seasonFolder };

        Assert.Equal(Path.Combine(seriesFolder, "tvshow.nfo"), PathResolver.GetXmlPathInfoForItem(series, modeRead: true));
        Assert.Equal(Path.Combine(seasonFolder, "season.nfo"), PathResolver.GetXmlPathInfoForItem(season, modeRead: true));
    }

    [Fact]
    public void GetXmlPathInfoForItem_ForEpisode_RespectsUseSeasonDateForEpisodesAndModeRead()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            UseSeasonDateForEpisodes = true
        });

        var seasonFolder = Path.Combine(helper.TempDir, "Show", "Season 01");
        Directory.CreateDirectory(seasonFolder);
        var episodeFile = Path.Combine(seasonFolder, "s01e01.mkv");
        File.WriteAllText(episodeFile, "video");

        var episode = new Episode { Path = episodeFile };

        // Read mode with UseSeasonDateForEpisodes = true returns season.nfo
        var readPath = PathResolver.GetXmlPathInfoForItem(episode, modeRead: true);
        Assert.Equal(Path.Combine(seasonFolder, "season.nfo"), readPath);

        // Write mode (modeRead: false) always writes to the episode's own NFO file
        var writePath = PathResolver.GetXmlPathInfoForItem(episode, modeRead: false);
        Assert.Equal(Path.Combine(seasonFolder, "s01e01.nfo"), writePath);
    }
}
