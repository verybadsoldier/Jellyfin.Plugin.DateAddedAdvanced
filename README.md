<h1 align="center">Jellyfin DateAdded Advanced Plugin</h1>
<p align="center">

<img alt="Logo Banner" src="https://raw.githubusercontent.com/jellyfin/jellyfin-ux/master/branding/SVG/banner-logo-solid.svg?sanitize=true"/>
</p>

## About

This plugin adds advanced capabilities to control the DateAdded property of library items and also fixes one shortcoming of the default Jellyfin behavior.
By default, Jellyfin uses the created date from file and directory attributes for the DateAdded property. The DateAdded is a useful information worth preserving and the file attributes might get changed unintentionally over the years. So they might get lost when having to reset the Jeyllfin database.
On Linux, due to the behavior of the .NET framework, Jellyfin is not acquiring the created date correctly and is wrongly reporting modified date instead as reported here:
https://github.com/jellyfin/jellyfin/issues/10655

With this plugin, the source for the DateAdded property in Jellyfin is changed. The plugin can also store and read DateAdded to and from NFO files. Also, the bug described above gets fixed when using the plugin and the correct create date is used by Jellyin also on Linux (when supported by the filesystem used).

When the plugin is installed and a library scan is performed, then this will be happen related to DateAdded properties. When scanning, NFO files will always have priority over file and directory filesystem attributes. So, when an NFO file is found, then the `dateadded` node is read and used.
If no NFO file is found, then the default behavior of Jellyfin is changed in the following ways:

### For files (movies, songs, episodes etc.):
It can be configured which file date should be used:
* Creation date
* Modification date
* Oldest from creation and modification date
* Newest from creation and modification date

Reasoning is that for files that do not get changed (movies, TV show episodes), the modified date is quite stable and usually does not change on the filesystem. So, even after years the modification date might be the date the file was added to your collection.
For files that do change (e.g. music files due to re-tagging), the modified date usually does not tell when this file was added to your collection.
So, e.g. the DateAdded source can be configured to `Oldest` so Jellyfin will automatically use created date _OR_ modified date depending on which one is older.

### For folders (TV shows, TV show season, Music albums, Music artists etc.):
Automatically the oldest date of all contained files will be used. This means, e.g. for a music album, all music files will be considered and the oldest date will be used as DateAdded for the album.


## NFO
Jellyfin is capable of reading and writing NFO files on its own. But the content of NFO files will only be considered under certain conditions. With this plugin, the `dateadded` data will always be used from NFO files. Currently, these NFO files are supported:
* tvshow.nfo
* season.nfo
* album.nfo
* artist.nfo
* {tv show episode filename}.nfo

Newly created NFO files by this plugin, will only contain `dateadded`. E.g.:
```
<album>
  <dateadded>2001-04-03</dateadded>
</album>
```

### Option: RenameExistingMisformedNfos (available in >= 2.1.0.0)
It may be the case that when scanning media files there are already existing .nfo files which are not Jellyfin XML files. When using the option `RenameExistingMisformedNfos`, such files will be renamed by appending a `.bak` suffix. Then, a new and proper NFO file can be created to store the `dateadded` information.

## Build & Installation Process

1. Clone this repository

2. Ensure you have .NET Core SDK setup and installed

3. Build the plugin with following command:

```bash
dotnet publish --configuration Release --output bin
```

4. Place the resulting `Jellyfin.Plugin.Bookshelf.dll` file in a folder called `plugins/Jellyfin.Plugin.Bookshelf` inside your Jellyfin installation / data directory.