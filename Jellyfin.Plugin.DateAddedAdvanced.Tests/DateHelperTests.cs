using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Plugins.NfoCreateDate.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.DateAddedAdvanced.Tests;

[Collection("PluginTests")]
public class DateHelperTests
{
    [Fact]
    public void ResolveDateCreatedFromFile_WithCreatedStrategy_SelectsCreationTime()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created
        });

        var created = new DateTime(2021, 2, 10, 12, 0, 0, DateTimeKind.Utc);
        var modified = new DateTime(2024, 5, 20, 18, 0, 0, DateTimeKind.Utc);
        var movie = new Movie { Path = @"C:\Movies\test.mkv" };

        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, created, modified);
        var logger = new Mock<ILogger>();
        var dateHelper = new DateHelper(fileSystem.Object, logger.Object);

        var result = dateHelper.ResolveDateCreatedFromFile(movie);

        Assert.NotNull(result);
        Assert.Equal(created, result.Value);
    }

    [Fact]
    public void ResolveDateCreatedFromFile_WithModifiedStrategy_SelectsModifiedTime()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Modified
        });

        var created = new DateTime(2021, 2, 10, 12, 0, 0, DateTimeKind.Utc);
        var modified = new DateTime(2024, 5, 20, 18, 0, 0, DateTimeKind.Utc);
        var movie = new Movie { Path = @"C:\Movies\test.mkv" };

        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, created, modified);
        var logger = new Mock<ILogger>();
        var dateHelper = new DateHelper(fileSystem.Object, logger.Object);

        var result = dateHelper.ResolveDateCreatedFromFile(movie);

        Assert.NotNull(result);
        Assert.Equal(modified, result.Value);
    }

    [Fact]
    public void ResolveDateCreatedFromFile_WithOldestStrategy_SelectsEarlierTimestamp()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Oldest
        });

        var created = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var modified = new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var movie = new Movie { Path = @"C:\Movies\test.mkv" };

        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, created, modified);
        var logger = new Mock<ILogger>();
        var dateHelper = new DateHelper(fileSystem.Object, logger.Object);

        var result = dateHelper.ResolveDateCreatedFromFile(movie);

        Assert.NotNull(result);
        Assert.Equal(modified, result.Value);
    }

    [Fact]
    public void ResolveDateCreatedFromFile_WithNewestStrategy_SelectsLaterTimestamp()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Newest
        });

        var created = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var modified = new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var movie = new Movie { Path = @"C:\Movies\test.mkv" };

        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, created, modified);
        var logger = new Mock<ILogger>();
        var dateHelper = new DateHelper(fileSystem.Object, logger.Object);

        var result = dateHelper.ResolveDateCreatedFromFile(movie);

        Assert.NotNull(result);
        Assert.Equal(created, result.Value);
    }

    [Fact]
    public void ResolveDateCreatedFromFile_WithCurrentStrategy_SelectsCurrentTimeTruncatedToSeconds()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Current
        });

        var movie = new Movie { Path = @"C:\Movies\test.mkv" };
        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, DateTime.MinValue, DateTime.MinValue);
        var logger = new Mock<ILogger>();
        var dateHelper = new DateHelper(fileSystem.Object, logger.Object);

        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = dateHelper.ResolveDateCreatedFromFile(movie);
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.NotNull(result);
        Assert.Equal(0, result.Value.Millisecond);
        Assert.True(result.Value >= new DateTime(before.Ticks - (before.Ticks % TimeSpan.TicksPerSecond), before.Kind));
        Assert.True(result.Value <= after);
    }

    [Fact]
    public void ResolveDateCreatedFromFile_TruncatesMillisecondsToZero()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created
        });

        // 12:00:00.852Z
        var createdWithMs = new DateTime(2021, 2, 10, 12, 0, 0, 852, DateTimeKind.Utc);
        var movie = new Movie { Path = @"C:\Movies\test.mkv" };

        var fileSystem = TestPluginHelper.CreateMockFileSystem(movie.Path, createdWithMs, createdWithMs);
        var logger = new Mock<ILogger>();
        var dateHelper = new DateHelper(fileSystem.Object, logger.Object);

        var result = dateHelper.ResolveDateCreatedFromFile(movie);

        Assert.NotNull(result);
        Assert.Equal(0, result.Value.Millisecond);
        Assert.Equal(new DateTime(2021, 2, 10, 12, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public void ResolveDateCreatedFromFile_DistinguishesAudioAndVideoConfigurations()
    {
        using var helper = new TestPluginHelper(new PluginConfiguration
        {
            DateAddedSourceVideo = PluginConfiguration.DateSource.Created,
            DateAddedSourceAudio = PluginConfiguration.DateSource.Modified
        });

        var created = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var modified = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var movie = new Movie { Path = @"C:\Media\movie.mkv" };
        var audio = new Audio { Path = @"C:\Media\song.mp3" };

        var mockFs = new Mock<IFileSystem>();
        mockFs.Setup(fs => fs.GetFileInfo(movie.Path)).Returns(new FileSystemMetadata { FullName = movie.Path });
        mockFs.Setup(fs => fs.GetFileInfo(audio.Path)).Returns(new FileSystemMetadata { FullName = audio.Path });
        mockFs.Setup(fs => fs.GetCreationTimeUtc(It.IsAny<FileSystemMetadata>())).Returns(created);
        mockFs.Setup(fs => fs.GetLastWriteTimeUtc(It.IsAny<FileSystemMetadata>())).Returns(modified);

        var logger = new Mock<ILogger>();
        var dateHelper = new DateHelper(mockFs.Object, logger.Object);

        var movieDate = dateHelper.ResolveDateCreatedFromFile(movie);
        var audioDate = dateHelper.ResolveDateCreatedFromFile(audio);

        Assert.Equal(created, movieDate);
        Assert.Equal(modified, audioDate);
    }
}
