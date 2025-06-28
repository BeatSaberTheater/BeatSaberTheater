﻿using System.Threading;
using System.Threading.Tasks;
using BetterSongList.FilterModels;
using BetterSongList.Interfaces;

namespace BeatSaberTheater.Settings;

public class HasVideoFilter : IFilter, ITransformerPlugin
{
    public bool isReady => true;
    public string name => "Theater";
    public bool visible { get; } = true; //Plugin.Enabled && SettingsStore.Instance.PluginEnabled;

    public bool GetValueFor(BeatmapLevel level)
    {
        // return VideoLoader.MapsWithVideo.TryGetValue(level.levelID, out _);
        return false;
    }

    public Task Prepare(CancellationToken cancelToken)
    {
        return Task.CompletedTask;
    }

    public void ContextSwitch(SelectLevelCategoryViewController.LevelCategory levelCategory, BeatmapLevelPack? playlist)
    {
        //Not needed
    }
}