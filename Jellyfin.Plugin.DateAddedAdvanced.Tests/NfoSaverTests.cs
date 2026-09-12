using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.NfoCreateDate.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.DateAddedAdvanced.Tests;

[Collection("PluginTests")]
public class NfoSaverTests
{
    private static Movie CreateTestMovie(string folderPath, string movieFileName = "matrix.mkv")
    {
        var moviePath = Path.Combine(folderPath, movieFileName);
        if (!File.Exists(moviePath))
        {
            File.WriteAllText(moviePath, "dummy video data");
        }

        return new Movie
        {
            Name = "The Matrix",
            Path = moviePath,
            IsInMixedFolder = false
        };
    }

    [Fact]
    public async Task SaveAsync_WhenNfoContainsDateAdded_PreservesOriginalDateAndDoesNotModifyFile()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            AddDateToExistingNfos = true,
            WriteMovieNfo = true
        });

        var movie = CreateTestMovie(helper.TempDir);
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        var originalXml = "<movie><title>The Matrix</title><dateadded>2019-01-01 12:00:00Z</dateadded></movie>";
        File.WriteAllText(nfoPath, originalXml);

        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var logger = new Mock<ILogger<NfoSaver>>();
        var saver = new NfoSaver(fileSystem.Object, logger.Object);

        await saver.SaveAsync(movie, CancellationToken.None);

        var savedContent = File.ReadAllText(nfoPath);
        Assert.Equal(originalXml, savedContent);
    }

    [Fact]
    public async Task SaveAsync_WhenNfoMissingDateAdded_AndAddDateToExistingNfosIsTrue_InjectsDateAdded()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            AddDateToExistingNfos = true,
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created,
            WriteMovieNfo = true
        });

        var movie = CreateTestMovie(helper.TempDir);
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        var initialXml = "<movie><title>The Matrix</title><plot>A computer hacker learns about reality.</plot></movie>";
        File.WriteAllText(nfoPath, initialXml);

        var fileCreationTime = new DateTime(2021, 3, 4, 15, 30, 0, DateTimeKind.Utc);
        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, fileCreationTime, fileCreationTime.AddDays(1));
        var logger = new Mock<ILogger<NfoSaver>>();
        var saver = new NfoSaver(fileSystem.Object, logger.Object);

        await saver.SaveAsync(movie, CancellationToken.None);

        var doc = new XmlDocument();
        doc.Load(nfoPath);

        var dateAddedNode = doc.DocumentElement?.SelectSingleNode("/movie/dateadded");
        var titleNode = doc.DocumentElement?.SelectSingleNode("/movie/title");
        var plotNode = doc.DocumentElement?.SelectSingleNode("/movie/plot");

        Assert.NotNull(dateAddedNode);
        Assert.Equal("2021-03-04 15:30:00Z", dateAddedNode.InnerText);
        Assert.NotNull(titleNode);
        Assert.Equal("The Matrix", titleNode.InnerText);
        Assert.NotNull(plotNode);
        Assert.Equal("A computer hacker learns about reality.", plotNode.InnerText);
    }

    [Fact]
    public async Task SaveAsync_WhenNfoMissingDateAdded_AndAddDateToExistingNfosIsFalse_LeavesFileUntouched()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            AddDateToExistingNfos = false,
            WriteMovieNfo = true
        });

        var movie = CreateTestMovie(helper.TempDir);
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        var initialXml = "<movie><title>The Matrix</title></movie>";
        File.WriteAllText(nfoPath, initialXml);

        var fileCreationTime = new DateTime(2021, 3, 4, 15, 30, 0, DateTimeKind.Utc);
        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, fileCreationTime, fileCreationTime);
        var logger = new Mock<ILogger<NfoSaver>>();
        var saver = new NfoSaver(fileSystem.Object, logger.Object);

        await saver.SaveAsync(movie, CancellationToken.None);

        var savedContent = File.ReadAllText(nfoPath);
        Assert.Equal(initialXml, savedContent);
    }

    [Fact]
    public async Task SaveAsync_WhenNfoDoesNotExist_CreatesNewNfoWithDateAdded()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created,
            WriteMovieNfo = true
        });

        var movie = CreateTestMovie(helper.TempDir);
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        Assert.False(File.Exists(nfoPath));

        var fileCreationTime = new DateTime(2022, 7, 8, 18, 0, 0, DateTimeKind.Utc);
        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, fileCreationTime, fileCreationTime);
        var logger = new Mock<ILogger<NfoSaver>>();
        var saver = new NfoSaver(fileSystem.Object, logger.Object);

        await saver.SaveAsync(movie, CancellationToken.None);

        Assert.True(File.Exists(nfoPath));
        var doc = new XmlDocument();
        doc.Load(nfoPath);

        Assert.Equal("movie", doc.DocumentElement?.Name);
        var dateAddedNode = doc.DocumentElement?.SelectSingleNode("/movie/dateadded");
        Assert.NotNull(dateAddedNode);
        Assert.Equal("2022-07-08 18:00:00Z", dateAddedNode.InnerText);
    }

    [Fact]
    public async Task SaveAsync_WhenNfoIsMisformed_AndRenameExistingMisformedNfosIsTrue_BacksUpAndRecreates()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            RenameExistingMisformedNfos = true,
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created,
            WriteMovieNfo = true
        });

        var movie = CreateTestMovie(helper.TempDir);
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        var badXml = "THIS IS NOT XML CONTENT <<>>";
        File.WriteAllText(nfoPath, badXml);

        var fileCreationTime = new DateTime(2023, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, fileCreationTime, fileCreationTime);
        var logger = new Mock<ILogger<NfoSaver>>();
        var saver = new NfoSaver(fileSystem.Object, logger.Object);

        await saver.SaveAsync(movie, CancellationToken.None);

        var bakPath = nfoPath + ".bak";
        Assert.True(File.Exists(bakPath));
        Assert.Equal(badXml, File.ReadAllText(bakPath));

        Assert.True(File.Exists(nfoPath));
        var doc = new XmlDocument();
        doc.Load(nfoPath);
        Assert.Equal("movie", doc.DocumentElement?.Name);
        var dateNode = doc.DocumentElement?.SelectSingleNode("/movie/dateadded");
        Assert.NotNull(dateNode);
        Assert.Equal("2023-01-15 10:00:00Z", dateNode.InnerText);
    }

    [Fact]
    public async Task SaveAsync_WhenNfoIsMisformed_AndRenameExistingMisformedNfosIsFalse_LeavesFileUntouched()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            RenameExistingMisformedNfos = false,
            WriteMovieNfo = true
        });

        var movie = CreateTestMovie(helper.TempDir);
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        var badXml = "THIS IS NOT XML CONTENT <<>>";
        File.WriteAllText(nfoPath, badXml);

        var fileCreationTime = new DateTime(2023, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, fileCreationTime, fileCreationTime);
        var logger = new Mock<ILogger<NfoSaver>>();
        var saver = new NfoSaver(fileSystem.Object, logger.Object);

        await saver.SaveAsync(movie, CancellationToken.None);

        var bakPath = nfoPath + ".bak";
        Assert.False(File.Exists(bakPath));
        Assert.Equal(badXml, File.ReadAllText(nfoPath));
    }

    [Fact]
    public void IsEnabledFor_RespectsConfigurationFlags()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            WriteMovieNfo = false,
            WriteEpisodeNfo = true,
            WriteTvShowNfo = false,
            WriteSeasonNfo = true,
            WriteArtistNfo = false
        });

        var fileSystem = new Mock<IFileSystem>();
        var logger = new Mock<ILogger<NfoSaver>>();
        var saver = new NfoSaver(fileSystem.Object, logger.Object);

        var movie = new Movie();
        var episode = new Episode();
        var series = new Series();
        var season = new Season();
        var artist = new MusicArtist();
        var album = new MusicAlbum();

        Assert.False(saver.IsEnabledFor(movie, ItemUpdateType.None));
        Assert.True(saver.IsEnabledFor(episode, ItemUpdateType.None));
        Assert.False(saver.IsEnabledFor(series, ItemUpdateType.None));
        Assert.True(saver.IsEnabledFor(season, ItemUpdateType.None));
        Assert.False(saver.IsEnabledFor(artist, ItemUpdateType.None));
        Assert.True(saver.IsEnabledFor(album, ItemUpdateType.None)); // MusicAlbum is always enabled
    }

    [Fact]
    public async Task SaveAsync_WhenEpisode_WritesEpisodedetailsRootAndDateAddedEvenWhenUseSeasonDateIsTrue()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            UseSeasonDateForEpisodes = true,
            WriteEpisodeNfo = true,
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created
        });

        var episodeFile = Path.Combine(helper.TempDir, "S01E01.mkv");
        File.WriteAllText(episodeFile, "video data");

        var episode = new Episode
        {
            Name = "Pilot",
            Path = episodeFile
        };

        var episodeNfoPath = Path.Combine(helper.TempDir, "S01E01.nfo");
        Assert.False(File.Exists(episodeNfoPath));

        var creationTime = new DateTime(2023, 5, 1, 10, 0, 0, DateTimeKind.Utc);
        var fileSystem = TestPluginHelper.CreateMockFileSystem(episode.Path, creationTime, creationTime);
        var saver = new NfoSaver(fileSystem.Object, new Mock<ILogger<NfoSaver>>().Object);

        await saver.SaveAsync(episode, CancellationToken.None);

        Assert.True(File.Exists(episodeNfoPath));
        var doc = new XmlDocument();
        doc.Load(episodeNfoPath);

        Assert.Equal("episodedetails", doc.DocumentElement?.Name);
        var dateNode = doc.DocumentElement?.SelectSingleNode("/episodedetails/dateadded");
        Assert.NotNull(dateNode);
        Assert.Equal("2023-05-01 10:00:00Z", dateNode.InnerText);
    }
}
