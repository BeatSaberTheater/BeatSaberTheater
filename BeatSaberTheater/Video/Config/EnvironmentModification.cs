using BeatSaberTheater.Environment;
using BeatSaberTheater.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace BeatSaberTheater.Video.Config;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class EnvironmentModification
{
    [JsonRequired] public string? name;
    public string? parentName;
    public string? cloneFrom;

    [JsonIgnore] public GameObject? gameObject;
    [JsonIgnore] public EnvironmentObject? gameObjectClone;

    public bool? active;
    public SerializableVector3? position;
    public SerializableVector3? rotation;
    public SerializableVector3? scale;
}