using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BeatSaberTheater.Download;

// ReSharper disable once InconsistentNaming
public class YTFormat
{
    public string? Quality;
    public int? Height;
    public int? Width;
    public string? AudioCodec;
    public string? VideoCodec;
    public string? FileExtension;
    public string? URL;
    public float? FramesPerSecond;
    public long? FileSize;

    public YTFormat(JsonNode? jToken)
    {
        if (jToken is null || (jToken["acodec"] == null && jToken["vcodec"] == null))
            throw new ArgumentException("Invalid format");

        Quality = jToken["format_note"]?.GetValue<string?>();
        FileSize = jToken["filesize"]?.GetValue<long?>();
        Width = jToken["width"]?.GetValue<int?>();
        Height = jToken["height"]?.GetValue<int?>();
        AudioCodec = jToken["acodec"]?.GetValue<string?>();
        VideoCodec = jToken["vcodec"]?.GetValue<string?>();
        FileExtension = jToken["ext"]?.GetValue<string?>();
        URL = jToken["url"]?.GetValue<string?>();
        FramesPerSecond = jToken["fps"]?.GetValue<float?>();
    }
}