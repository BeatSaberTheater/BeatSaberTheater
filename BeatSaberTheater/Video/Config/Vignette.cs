using Newtonsoft.Json;

namespace BeatSaberTheater.Video.Config;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class Vignette
{
    public string? type;
    public float? radius;
    public float? softness;
}