using Newtonsoft.Json;

namespace BeatSaberTheater.Video;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class ColorCorrection
{
    public float? brightness;
    public float? contrast;
    public float? saturation;
    public float? hue;
    public float? exposure;
    public float? gamma;
}