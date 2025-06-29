using Newtonsoft.Json;

namespace BeatSaberTheater.Video;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class Vignette
{
    public string? type;
    public float? radius;
    public float? softness;
}