# BeatSaberTheater

This is a Beat Saber mod that will play a video in the background of your maps. Use this to see the music
videos for your favorite songs while you play!

This mod is a rewrite/successor to the [Cinema mod](https://github.com/Kevga/BeatSaberCinema).

## Installation

This mod is not yet available on BeatMods and must be installed manually. 

### Manual Installation

1. Download the latest release from [here](https://github.com/neboman11/BeatSaberTheater/releases).
2. Copy the contents of the zip into your current Beat Saber directory (the one with `Beat Saber.exe`).
3. That's it! Theater is now installed

### Dependencies

This mod has several dependencies, ensure they are all installed before launching the game. These dependencies
can be installed through your favorite mod manager (BSManager, ModAssistant, etc.).

* BSIPA
* SongCore
* BS Utils
* SiraUtil
* BeatSaberPlaylistsLib
* ffmpeg
* yt-dlp
* BeatSaberMarkupLanguage
* BetterSongList

## Usage

Before you can see the videos for you songs, you must download them. Some songs are preconfigured with a video and
you will see a download button for them as shown below.

![Preconfigured Video Example](https://github.com/neboman11/BeatSaberTheater/blob/main/docs/images/preconfigured-video-example.png)

If no video is found, you can search for one to use in the `Gameplay Setup` menu on the left. Simply go to the
`Mods` tab, and then open the `Theater` menu.

![Search for Video Example](https://github.com/neboman11/BeatSaberTheater/blob/main/docs/images/search-for-video-example.png)

After you select `Search`, Theater will search YouTube for videos matching the selected song. Results will begin to
appear in a list as Theater finds them. Choose whichever video you want, and then hit download.

![Search Download Example](https://github.com/neboman11/BeatSaberTheater/blob/main/docs/images/search-download-example.png)

After the song downloads, you will be given a menu to adjust the timing of the video with the song. This will
allow you to sync them together and skip any non-music sections at the beginning of the video. Hitting the `Preview`
button will play both the video and song from the beginning, with the Beat Saber song playing only in your right ear
and the video audio playing in your left ear.
