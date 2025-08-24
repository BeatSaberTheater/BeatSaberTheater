using BeatSaberTheater.Models;
using Newtonsoft.Json;

namespace BeatSaberTheater.Video.Config;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class ScreenConfig
{
    public SerializableVector3? position;
    public SerializableVector3? rotation;
    public SerializableVector3? scale;
}