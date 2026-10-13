using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatSaberTheater.Settings;

public static class VideoFormats
{
	public enum Format
	{
		Mp4 = 0,
		// VP8 video in a webm container
		Webm = 1,
		// VP9 video in an mp4 container
		Vp9 = 2,
	}

	// Ordered by increasing compatibility, which is also the order shown in the dropdown
	private static readonly (Format Format, string Name)[] DisplayNames =
	{
		(Format.Mp4, "MP4 (H.264)"),
		(Format.Vp9, "MP4 (VP9)"),
		(Format.Webm, "WebM (VP8)"),
	};

	public static string ToName(Format format)
	{
		return DisplayNames.First(entry => entry.Format == format).Name;
	}

	public static List<object> GetFormatList()
	{
		return DisplayNames.Select(entry => (object)entry.Name).ToList();
	}

	public static Format FromName(string mode)
	{
		return DisplayNames.First(entry => entry.Name == mode).Format;
	}

	// Vp9 gets its own suffix so it doesn't collide with the Mp4 download of the same video
	public static string GetFileExtension(Format format)
	{
		return format == Format.Vp9 ? ".vp9.mp4" : "." + format.ToString().ToLower();
	}
}