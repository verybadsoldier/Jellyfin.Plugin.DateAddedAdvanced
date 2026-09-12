using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.NfoCreateDate.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.DateAddedAdvanced.Tests;

[Collection("PluginTests")]
public class NfoCreateDateProviderTests
{
    private static Movie CreateTestMovie(string folderPath, string movieFileName = "inception.mkv")
    {
        var moviePath = Path.Combine(folderPath, movieFileName);
        if (!File.Exists(moviePath))
        {
            File.WriteAllText(moviePath, "dummy video data");
        }

        return new Movie
        {
            Name = "Inception",
            Path = moviePath,
            IsInMixedFolder = false,
            DateCreated = new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc)
        };
    }

    [Fact]
    public async Task FetchAsync_WhenNfoContainsDateAdded_ParsesAndUpdatesItemDateCreated()
    {
        using var helper = new TestPluginHelper();

        var movie = CreateTestMovie(helper.TempDir);
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        File.WriteAllText(nfoPath, "<movie><dateadded>2020-05-10 14:22:33Z</dateadded></movie>");

        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, DateTime.UtcNow, DateTime.UtcNow);
        var logger = new Mock<ILogger<NfoCreateDateProvider>>();
        var libraryManager = new Mock<ILibraryManager>();
        var provider = new NfoCreateDateProvider(libraryManager.Object, logger.Object, fileSystem.Object);

        var result = await provider.FetchAsync(movie, new MetadataRefreshOptions(new DirectoryService(fileSystem.Object)), CancellationToken.None);

        Assert.Equal(ItemUpdateType.MetadataEdit, result);
        Assert.Equal(new DateTime(2020, 5, 10, 14, 22, 33, DateTimeKind.Utc), movie.DateCreated);
    }

    [Fact]
    public async Task FetchAsync_WhenNfoContainsSameDateAdded_ReturnsItemUpdateTypeNone()
    {
        using var helper = new TestPluginHelper();

        var movie = CreateTestMovie(helper.TempDir);
        var existingDate = new DateTime(2020, 5, 10, 14, 22, 33, DateTimeKind.Utc);
        movie.DateCreated = existingDate;

        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        File.WriteAllText(nfoPath, "<movie><dateadded>2020-05-10 14:22:33Z</dateadded></movie>");

        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, DateTime.UtcNow, DateTime.UtcNow);
        var logger = new Mock<ILogger<NfoCreateDateProvider>>();
        var libraryManager = new Mock<ILibraryManager>();
        var provider = new NfoCreateDateProvider(libraryManager.Object, logger.Object, fileSystem.Object);

        var result = await provider.FetchAsync(movie, new MetadataRefreshOptions(new DirectoryService(fileSystem.Object)), CancellationToken.None);

        Assert.Equal(ItemUpdateType.None, result);
        Assert.Equal(existingDate, movie.DateCreated);
    }

    [Fact]
    public async Task FetchAsync_WhenNfoMissingDateAdded_ResolvesDateFromFileAttributes()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created
        });

        var movie = CreateTestMovie(helper.TempDir);
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        File.WriteAllText(nfoPath, "<movie><title>Inception</title></movie>");

        var fileCreationTime = new DateTime(2022, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, fileCreationTime, fileCreationTime.AddDays(1));
        var logger = new Mock<ILogger<NfoCreateDateProvider>>();
        var libraryManager = new Mock<ILibraryManager>();
        var provider = new NfoCreateDateProvider(libraryManager.Object, logger.Object, fileSystem.Object);

        var result = await provider.FetchAsync(movie, new MetadataRefreshOptions(new DirectoryService(fileSystem.Object)), CancellationToken.None);

        Assert.Equal(ItemUpdateType.MetadataEdit, result);
        Assert.Equal(fileCreationTime, movie.DateCreated);
    }

    [Fact]
    public async Task FetchAsync_WhenNfoDoesNotExist_ResolvesDateFromFileAttributes()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created
        });

        var movie = CreateTestMovie(helper.TempDir);
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        Assert.False(File.Exists(nfoPath));

        var fileCreationTime = new DateTime(2023, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, fileCreationTime, fileCreationTime.AddDays(1));
        var logger = new Mock<ILogger<NfoCreateDateProvider>>();
        var libraryManager = new Mock<ILibraryManager>();
        var provider = new NfoCreateDateProvider(libraryManager.Object, logger.Object, fileSystem.Object);

        var result = await provider.FetchAsync(movie, new MetadataRefreshOptions(new DirectoryService(fileSystem.Object)), CancellationToken.None);

        Assert.Equal(ItemUpdateType.MetadataEdit, result);
        Assert.Equal(fileCreationTime, movie.DateCreated);
    }

    [Fact]
    public async Task FetchAsync_WhenNfoHasInvalidDateString_ReturnsNoneWithoutCrashing()
    {
        using var helper = new TestPluginHelper();

        var movie = CreateTestMovie(helper.TempDir);
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        File.WriteAllText(nfoPath, "<movie><dateadded>this-is-not-a-date</dateadded></movie>");

        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, DateTime.UtcNow, DateTime.UtcNow);
        var logger = new Mock<ILogger<NfoCreateDateProvider>>();
        var libraryManager = new Mock<ILibraryManager>();
        var provider = new NfoCreateDateProvider(libraryManager.Object, logger.Object, fileSystem.Object);

        var result = await provider.FetchAsync(movie, new MetadataRefreshOptions(new DirectoryService(fileSystem.Object)), CancellationToken.None);

        Assert.Equal(ItemUpdateType.None, result);
    }

    [Fact]
    public async Task FetchAsync_WhenNfoHasCorruptXml_SafelyFallsBackToFileAttributes()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created
        });

        var movie = CreateTestMovie(helper.TempDir);
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        File.WriteAllText(nfoPath, "<<<BROKEN XML NOT PARSABLE>>>");

        var fileCreationTime = new DateTime(2022, 3, 10, 8, 30, 0, DateTimeKind.Utc);
        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, fileCreationTime, fileCreationTime);
        var logger = new Mock<ILogger<NfoCreateDateProvider>>();
        var libraryManager = new Mock<ILibraryManager>();
        var provider = new NfoCreateDateProvider(libraryManager.Object, logger.Object, fileSystem.Object);

        var result = await provider.FetchAsync(movie, new MetadataRefreshOptions(new DirectoryService(fileSystem.Object)), CancellationToken.None);

        // When XML is broken, ReadDateAdded catches the error and returns null, falling back to resolving from file
        Assert.Equal(ItemUpdateType.MetadataEdit, result);
        Assert.Equal(fileCreationTime, movie.DateCreated);
    }

    [Fact]
    public async Task FetchAsync_WhenItemPathDoesNotExist_ReturnsNone()
    {
        using var helper = new TestPluginHelper();

        var movie = new Movie
        {
            Name = "Missing Movie",
            Path = @"C:\NonExistent\Folder\Movie.mkv"
        };

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists(movie.Path)).Returns(false);
        fileSystem.Setup(fs => fs.DirectoryExists(movie.Path)).Returns(false);

        var logger = new Mock<ILogger<NfoCreateDateProvider>>();
        var libraryManager = new Mock<ILibraryManager>();
        var provider = new NfoCreateDateProvider(libraryManager.Object, logger.Object, fileSystem.Object);

        var result = await provider.FetchAsync(movie, new MetadataRefreshOptions(new DirectoryService(fileSystem.Object)), CancellationToken.None);

        Assert.Equal(ItemUpdateType.None, result);
    }

    [Fact]
    public void ReadDateAdded_HandlesVariedRootsAndMissingTags()
    {
        using var helper = new TestPluginHelper();
        var nfoPath = Path.Combine(helper.TempDir, "test.nfo");

        // Movie
        File.WriteAllText(nfoPath, "<movie><dateadded>2021-01-01 10:00:00Z</dateadded></movie>");
        Assert.Equal("2021-01-01 10:00:00Z", NfoCreateDateProvider.ReadDateAdded(nfoPath, "movie"));

        // Episode details
        File.WriteAllText(nfoPath, "<episodedetails><dateadded>2021-02-02 11:00:00Z</dateadded></episodedetails>");
        Assert.Equal("2021-02-02 11:00:00Z", NfoCreateDateProvider.ReadDateAdded(nfoPath, "episodedetails"));

        // Wrong root name
        Assert.Null(NfoCreateDateProvider.ReadDateAdded(nfoPath, "movie"));

        // Missing dateadded node
        File.WriteAllText(nfoPath, "<movie><title>No date</title></movie>");
        Assert.Null(NfoCreateDateProvider.ReadDateAdded(nfoPath, "movie"));

        // Non-existent file
        Assert.Null(NfoCreateDateProvider.ReadDateAdded(Path.Combine(helper.TempDir, "doesnotexist.nfo"), "movie"));
    }

    [Theory]
    [InlineData("2021-05-10 14:22:33Z", 2021, 5, 10, 14, 22, 33)]
    [InlineData("2021-05-10T14:22:33Z", 2021, 5, 10, 14, 22, 33)]
    [InlineData("2021-05-10 14:22:33", 2021, 5, 10, 14, 22, 33)]
    [InlineData("2021-05-10", 2021, 5, 10, 0, 0, 0)]
    public async Task FetchAsync_WithVariousDateFormats_CorrectlyParsesDate(
        string dateString, int year, int month, int day, int hour, int minute, int second)
    {
        using var helper = new TestPluginHelper();

        var movieFolder = Path.Combine(helper.TempDir, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(movieFolder);
        var movie = CreateTestMovie(movieFolder, "movie.mkv");
        var nfoPath = PathResolver.GetXmlPathInfoForItem(movie, true);
        File.WriteAllText(nfoPath, $"<movie><dateadded>{dateString}</dateadded></movie>");

        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, DateTime.UtcNow, DateTime.UtcNow);
        var logger = new Mock<ILogger<NfoCreateDateProvider>>();
        var libraryManager = new Mock<ILibraryManager>();
        var provider = new NfoCreateDateProvider(libraryManager.Object, logger.Object, fileSystem.Object);

        var result = await provider.FetchAsync(movie, new MetadataRefreshOptions(new DirectoryService(fileSystem.Object)), CancellationToken.None);

        Assert.Equal(ItemUpdateType.MetadataEdit, result);
        Assert.Equal(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc), movie.DateCreated);
    }
}
