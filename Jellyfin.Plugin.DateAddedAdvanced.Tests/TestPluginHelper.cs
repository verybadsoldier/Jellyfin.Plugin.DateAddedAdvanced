using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Plugins.NfoCreateDate.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.DateAddedAdvanced.Tests;

[CollectionDefinition("PluginTests", DisableParallelization = true)]
public class PluginTestCollection
{
}

public class TestPluginHelper : IDisposable
{
    private readonly string _tempDir;

    public Plugin Plugin { get; }

    public string TempDir => _tempDir;

    public TestPluginHelper(PluginConfiguration? configuration = null)
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DateAddedAdvTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var appPaths = new Mock<IApplicationPaths>();
        appPaths.Setup(a => a.PluginConfigurationsPath).Returns(_tempDir);
        appPaths.Setup(a => a.ConfigurationDirectoryPath).Returns(_tempDir);
        appPaths.Setup(a => a.PluginsPath).Returns(_tempDir);
        appPaths.Setup(a => a.DataPath).Returns(_tempDir);
        appPaths.Setup(a => a.ProgramDataPath).Returns(_tempDir);

        var xmlSerializer = new Mock<IXmlSerializer>();
        var logger = new Mock<ILogger<Plugin>>();

        Plugin = new Plugin(appPaths.Object, xmlSerializer.Object, logger.Object);
        Plugin.UpdateConfiguration(configuration ?? new PluginConfiguration());
    }

    public string CreateTempFile(string relativePath, string content = "")
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public static Mock<IFileSystem> CreateMockFileSystem(string filePath, DateTime creationTimeUtc, DateTime lastWriteTimeUtc)
    {
        var mock = new Mock<IFileSystem>();
        var metadata = new FileSystemMetadata
        {
            FullName = filePath,
            Exists = true,
            CreationTimeUtc = creationTimeUtc,
            LastWriteTimeUtc = lastWriteTimeUtc
        };

        mock.Setup(fs => fs.GetFileInfo(filePath)).Returns(metadata);
        mock.Setup(fs => fs.GetCreationTimeUtc(It.IsAny<FileSystemMetadata>())).Returns(creationTimeUtc);
        mock.Setup(fs => fs.GetLastWriteTimeUtc(It.IsAny<FileSystemMetadata>())).Returns(lastWriteTimeUtc);
        mock.Setup(fs => fs.FileExists(filePath)).Returns(true);
        mock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        return mock;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup failure in temp directory
        }
    }
}
