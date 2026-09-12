using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.NfoCreateDate.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.DateAddedAdvanced.Tests;

/// <summary>
/// End-to-end workflow tests combining NfoCreateDateProvider and NfoSaver
/// to verify real-world Jellyfin library lifecycle and date added preservation.
/// </summary>
[Collection("PluginTests")]
public class DateAddedWorkflowTests
{
    private static Movie CreateMovie(string folder, string fileName)
    {
        var filePath = Path.Combine(folder, fileName);
        File.WriteAllText(filePath, "movie data");
        return new Movie
        {
            Name = Path.GetFileNameWithoutExtension(fileName),
            Path = filePath,
            IsInMixedFolder = false,
            DateCreated = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task QualityUpgrade_Issue7_PreservesOriginalDateAddedAcrossScans()
    {
        // Issue #7 scenario:
        // A movie was added in 2021. NFO exists with 2021 date.
        // In 2026, Radarr upgrades the MKV file (new file creation date: 2026-09-12).
        // During library refresh, Jellyfin must preserve the original 2021 date added
        // in both the database (item.DateCreated) and in the NFO file on disk.

        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created,
            AddDateToExistingNfos = true,
            WriteMovieNfo = true
        });

        var movie = CreateMovie(helper.TempDir, "gladiator.mkv");
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        var originalNfoContent = "<movie><title>Gladiator</title><dateadded>2021-04-10 14:00:00Z</dateadded></movie>";
        File.WriteAllText(nfoPath, originalNfoContent);

        // Upgraded file has a brand new 2026 timestamp
        var upgradedFileCreationTime = new DateTime(2026, 9, 12, 8, 30, 0, DateTimeKind.Utc);
        var mockFs = TestPluginHelper.CreateMockFileSystem(movie.Path, upgradedFileCreationTime, upgradedFileCreationTime);

        var providerLogger = new Mock<ILogger<NfoCreateDateProvider>>();
        var saverLogger = new Mock<ILogger<NfoSaver>>();
        var libraryManager = new Mock<ILibraryManager>();

        var provider = new NfoCreateDateProvider(libraryManager.Object, providerLogger.Object, mockFs.Object);
        var saver = new NfoSaver(mockFs.Object, saverLogger.Object);

        // 1. Metadata Refresh: Provider runs
        var updateType = await provider.FetchAsync(movie, new MetadataRefreshOptions(new DirectoryService(mockFs.Object)), CancellationToken.None);

        Assert.Equal(ItemUpdateType.MetadataEdit, updateType);
        Assert.Equal(new DateTime(2021, 4, 10, 14, 0, 0, DateTimeKind.Utc), movie.DateCreated);

        // 2. Metadata Save: Saver runs
        await saver.SaveAsync(movie, CancellationToken.None);

        // 3. Verify NFO file on disk was NOT overwritten by the 2026 upgrade timestamp
        var diskContent = File.ReadAllText(nfoPath);
        Assert.Equal(originalNfoContent, diskContent);

        var doc = new XmlDocument();
        doc.Load(nfoPath);
        var dateNode = doc.DocumentElement?.SelectSingleNode("/movie/dateadded");
        Assert.NotNull(dateNode);
        Assert.Equal("2021-04-10 14:00:00Z", dateNode.InnerText);
    }

    [Fact]
    public async Task ExistingNfoWithoutDateAdded_GetsEnrichedAndPreservedOnSubsequentScans()
    {
        // Scenario:
        // Existing NFO from another tool without <dateadded>.
        // Media file has 2019 timestamp.
        // First scan: Provider resolves date from file, Saver injects <dateadded> tag.
        // Second scan: Provider reads the injected <dateadded> tag directly.

        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created,
            AddDateToExistingNfos = true,
            WriteMovieNfo = true
        });

        var movie = CreateMovie(helper.TempDir, "interstellar.mkv");
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        File.WriteAllText(nfoPath, "<movie><title>Interstellar</title><plot>Space travel</plot></movie>");

        var fileCreationTime = new DateTime(2019, 11, 7, 20, 15, 0, DateTimeKind.Utc);
        var mockFs = TestPluginHelper.CreateMockFileSystem(movie.Path, fileCreationTime, fileCreationTime);

        var provider = new NfoCreateDateProvider(new Mock<ILibraryManager>().Object, new Mock<ILogger<NfoCreateDateProvider>>().Object, mockFs.Object);
        var saver = new NfoSaver(mockFs.Object, new Mock<ILogger<NfoSaver>>().Object);

        // First pass: Provider resolves from file since NFO lacks <dateadded>
        var updateType = await provider.FetchAsync(movie, new MetadataRefreshOptions(new DirectoryService(mockFs.Object)), CancellationToken.None);
        Assert.Equal(ItemUpdateType.MetadataEdit, updateType);
        Assert.Equal(fileCreationTime, movie.DateCreated);

        // First pass: Saver injects <dateadded> into existing NFO
        await saver.SaveAsync(movie, CancellationToken.None);

        var doc = new XmlDocument();
        doc.Load(nfoPath);
        var dateNode = doc.DocumentElement?.SelectSingleNode("/movie/dateadded");
        Assert.NotNull(dateNode);
        Assert.Equal("2019-11-07 20:15:00Z", dateNode.InnerText);
        Assert.NotNull(doc.DocumentElement?.SelectSingleNode("/movie/title"));
        Assert.NotNull(doc.DocumentElement?.SelectSingleNode("/movie/plot"));

        // Second pass: File attributes change (e.g. file touched)
        var touchedTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var mockFsTouched = TestPluginHelper.CreateMockFileSystem(movie.Path, touchedTime, touchedTime);
        var providerSecondPass = new NfoCreateDateProvider(new Mock<ILibraryManager>().Object, new Mock<ILogger<NfoCreateDateProvider>>().Object, mockFsTouched.Object);

        // Reset movie DateCreated to simulate reload from Jellyfin default
        movie.DateCreated = DateTime.UtcNow;
        var secondPassUpdate = await providerSecondPass.FetchAsync(movie, new MetadataRefreshOptions(new DirectoryService(mockFsTouched.Object)), CancellationToken.None);

        Assert.Equal(ItemUpdateType.MetadataEdit, secondPassUpdate);
        Assert.Equal(fileCreationTime, movie.DateCreated); // Still 2019, preserved from NFO!
    }

    [Fact]
    public async Task BrandNewMovie_WithoutNfo_CreatesNfoAndPreservesDatePermanently()
    {
        // Scenario:
        // Brand new movie added with no NFO.
        // First scan creates movie.nfo.
        // Subsequent scans never overwrite the created date.

        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created,
            WriteMovieNfo = true
        });

        var movie = CreateMovie(helper.TempDir, "avatar.mkv");
        var nfoPath = Path.Combine(helper.TempDir, "movie.nfo");
        Assert.False(File.Exists(nfoPath));

        var originalCreationTime = new DateTime(2022, 12, 16, 18, 0, 0, DateTimeKind.Utc);
        var mockFs = TestPluginHelper.CreateMockFileSystem(movie.Path, originalCreationTime, originalCreationTime);

        var provider = new NfoCreateDateProvider(new Mock<ILibraryManager>().Object, new Mock<ILogger<NfoCreateDateProvider>>().Object, mockFs.Object);
        var saver = new NfoSaver(mockFs.Object, new Mock<ILogger<NfoSaver>>().Object);

        // 1. First scan
        await provider.FetchAsync(movie, new MetadataRefreshOptions(new DirectoryService(mockFs.Object)), CancellationToken.None);
        await saver.SaveAsync(movie, CancellationToken.None);

        Assert.True(File.Exists(nfoPath));
        var doc = new XmlDocument();
        doc.Load(nfoPath);
        Assert.Equal("2022-12-16 18:00:00Z", doc.DocumentElement?.SelectSingleNode("/movie/dateadded")?.InnerText);

        // 2. Later, file is modified / touched
        var modifiedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var mockFsModified = TestPluginHelper.CreateMockFileSystem(movie.Path, modifiedTime, modifiedTime);
        var saverModified = new NfoSaver(mockFsModified.Object, new Mock<ILogger<NfoSaver>>().Object);

        await saverModified.SaveAsync(movie, CancellationToken.None);

        // File remains unchanged
        var docAfter = new XmlDocument();
        docAfter.Load(nfoPath);
        Assert.Equal("2022-12-16 18:00:00Z", docAfter.DocumentElement?.SelectSingleNode("/movie/dateadded")?.InnerText);
    }
}
