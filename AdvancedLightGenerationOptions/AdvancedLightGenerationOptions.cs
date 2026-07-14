// SPDX-License-Identifier: MIT
// Original work Copyright (c) 2024 Loloppe
// Modified work Copyright (c) 2025 Jonas00000
// Modified work Copyright (c) 2026 EklipZ

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;
using UnityEngine;
using Beatmap.Base;
using Beatmap.Base.Customs;

namespace AdvancedLightGenerationOptions
{
    [Plugin("AdvancedLightGenerationOptions")]
    public class AdvancedLightGenerationOptions
    {
        private UI _ui;
        private NoteGridContainer _noteGridContainer;
        private EventGridContainer _eventGridContainer;

        private static string GetVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version?.ToString() ?? "2.1.0.0";
        }

        [Init]
        private void Init()
        {
            PluginLogger.Log("Init() called");
            SceneManager.sceneLoaded += SceneLoaded;
            ConfigManager.Load();
            _ui = new UI(this);
            PluginLogger.Log("Init() complete");
        }

        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            PluginLogger.Log($"SceneLoaded: {arg0.name} buildIndex={arg0.buildIndex}");
            if (arg0.buildIndex != 3)
            {
                if (!SceneManager.GetSceneByBuildIndex(3).isLoaded)
                {
                    PluginLogger.Log("Editor scene unloaded, clearing menu");
                    _ui.ClearMenu();
                    _noteGridContainer = null;
                    _eventGridContainer = null;
                }
                else
                {
                    PluginLogger.Log("Skipping additive scene load");
                }

                return;
            }

            _noteGridContainer = UnityEngine.Object.FindFirstObjectByType<NoteGridContainer>();
            _eventGridContainer = UnityEngine.Object.FindFirstObjectByType<EventGridContainer>();

            var mapEditorUI = UnityEngine.Object.FindFirstObjectByType<MapEditorUI>();
            if (mapEditorUI != null)
            {
                PluginLogger.Log("MapEditorUI found, calling AddMenu");
                _ui.AddMenu(mapEditorUI);
            }
            else
            {
                PluginLogger.Log("MapEditorUI not found");
            }
        }

        public void Light()
        {
            PluginLogger.Log("Light() called");
            try
            {
                if (_noteGridContainer?.MapObjects == null || !_noteGridContainer.MapObjects.Any())
                {
                    PluginLogger.Log("Light() aborted: no notes");
                    PersistentUI.Instance.ShowDialogBox("No notes found! Please add notes to your map before generating lights.", null, PersistentUI.DialogBoxPresetType.Ok);
                    return;
                }

                var arcContainer = UnityEngine.Object.FindFirstObjectByType<ArcGridContainer>();
                var arcs = arcContainer?.MapObjects?.ToList() ?? new List<BaseArc>();
                var sliders = new List<BaseSlider>(arcs.Cast<BaseSlider>());

                var state = new MapEditorState
                {
                    Notes = _noteGridContainer.MapObjects.ToList(),
                    Obstacles = UnityEngine.Object.FindFirstObjectByType<ObstacleGridContainer>()?.MapObjects?.ToList() ?? new List<BaseObstacle>(),
                    Sliders = sliders,
                    ExistingBoosts = _eventGridContainer.MapObjects.Where(e => e.Type == 5).ToList(),
                    Bookmarks = (BeatSaberSongContainer.Instance?.Map?.Bookmarks) ?? (new List<BaseBookmark>()),
                };

                PluginLogger.Log($"Light() generating for {state.Notes.Count} notes, {state.Obstacles.Count} walls, {state.Sliders.Count} sliders");

                var toDelete = _eventGridContainer.MapObjects.Where(ev => !new List<int>() { 14, 15, 100 }.Contains(ev.Type)).ToList();
                foreach (var ev in toDelete) _eventGridContainer.DeleteObject(ev, false, false, inCollectionOfDeletes: true);
                PluginLogger.Log($"Light() removed {toDelete.Count} existing generated events");

                var cfg = ConfigManager.Data;
                var newEvents = EventGenerator.GenerateAll(state, cfg);

                PluginLogger.Log($"Light() generated {newEvents.Count} events");

                foreach (var ev in newEvents)
                {
                    ev.WriteCustom();
                    _eventGridContainer.SpawnObject(ev, false, false, true);
                }

                _eventGridContainer.DoPostObjectsSpawnedWorkflow();
                _eventGridContainer.LinkAllLightEvents();
                _eventGridContainer.RefreshPool(true);
                _eventGridContainer.RefreshEventsAppearance(newEvents);
                SelectionController.DeselectAll();
                SelectionController.SelectedObjects = new HashSet<BaseObject>(newEvents);
                SelectionController.OnSelectionChanged?.Invoke();
                BeatmapActionContainer.AddAction(
                    new StrobeGeneratorGenerationAction(newEvents.ToArray(), toDelete.ToArray()));
                PluginLogger.Log("Light() linked events, refreshed appearance, selected events, and added undo action");

                if (BeatSaberSongContainer.Instance?.Map != null)
                {
                    var map = BeatSaberSongContainer.Instance.Map;
                    var version = GetVersion();
                    string lighterFieldName = (map.MajorVersion == 2) ? "_lighter" : "lighter";

                    SimpleJSON.JSONNode lighterData;
                    if (map.CustomData.HasKey(lighterFieldName) && map.CustomData[lighterFieldName].IsObject)
                    {
                        lighterData = map.CustomData[lighterFieldName];
                    }
                    else
                    {
                        lighterData = new SimpleJSON.JSONObject();
                        map.CustomData[lighterFieldName] = lighterData;
                    }

                    var autoLighterData = new SimpleJSON.JSONObject();
                    autoLighterData["version"] = version;
                    lighterData["AdvancedLightGenerationOptions by EklipZ"] = autoLighterData;

                    if (map.MajorVersion == 4 && BeatSaberSongContainer.Instance?.MapDifficultyInfo != null)
                    {
                        var diffInfo = BeatSaberSongContainer.Instance.MapDifficultyInfo;
                        if (!diffInfo.Lighters.Contains("AdvancedLightGenerationOptions by EklipZ"))
                        {
                            diffInfo.Lighters.Add("AdvancedLightGenerationOptions by EklipZ");
                        }
                    }
                }

                ConfigManager.Save();
                PluginLogger.Log("Light() complete");
            }
            catch (Exception ex)
            {
                PluginLogger.Log($"Light() exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                PersistentUI.Instance.ShowDialogBox($"Light generation failed: {ex.Message}", null, PersistentUI.DialogBoxPresetType.Ok);
            }
        }

        public void SyncToAllDiffs()
        {
            if (BeatSaberSongContainer.Instance?.Info == null || BeatSaberSongContainer.Instance?.Map == null)
            {
                PersistentUI.Instance.ShowDialogBox("No map loaded!", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }

            var currentMap = BeatSaberSongContainer.Instance.Map;
            var info = BeatSaberSongContainer.Instance.Info;
            var currentDiffInfo = BeatSaberSongContainer.Instance.MapDifficultyInfo;

            var lightEvents = currentMap.Events.Where(ev => !new List<int>() { 14, 15, 100 }.Contains(ev.Type)).ToList();

            if (lightEvents.Count == 0)
            {
                PersistentUI.Instance.ShowDialogBox("No lights to sync! Generate lights first.", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }

            string lighterFieldName = currentMap.MajorVersion == 2 ? "_lighter" : "lighter";
            SimpleJSON.JSONNode lighterMetadata = null;
            if (currentMap.CustomData.HasKey(lighterFieldName) && currentMap.CustomData[lighterFieldName].IsObject)
                lighterMetadata = currentMap.CustomData[lighterFieldName].Clone();

            int syncedCount = 0;
            foreach (var diffSet in info.DifficultySets)
            {
                foreach (var diff in diffSet.Difficulties)
                {
                    if (diff == currentDiffInfo) continue;

                    try
                    {
                        var targetMap = BeatSaberSongUtils.GetMapFromInfoFiles(info, diff);
                        if (targetMap == null) continue;

                        var toRemove = targetMap.Events.Where(ev => !new List<int>() { 14, 15, 100 }.Contains(ev.Type)).ToList();
                        foreach (var ev in toRemove)
                        {
                            targetMap.Events.Remove(ev);
                        }

                        foreach (var lightEvent in lightEvents)
                        {
                            targetMap.Events.Add(lightEvent.Clone() as BaseEvent);
                        }

                        if (lighterMetadata != null)
                        {
                            string targetLighterFieldName = targetMap.MajorVersion == 2 ? "_lighter" : "lighter";
                            targetMap.CustomData[targetLighterFieldName] = lighterMetadata.Clone();
                        }

                        if (info.MajorVersion == 4 && currentDiffInfo.Lighters.Count > 0)
                        {
                            diff.Lighters.Clear();
                            foreach (var lighter in currentDiffInfo.Lighters)
                            {
                                diff.Lighters.Add(lighter);
                            }
                        }

                        targetMap.Save();
                        syncedCount++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Failed to sync lights to {diff.Difficulty} ({diffSet.Characteristic}): {ex.Message}");
                    }
                }
            }

            if (info.MajorVersion == 4) info.Save();

            if (syncedCount == 0)
            {
                PersistentUI.Instance.ShowDialogBox("No other difficulties found to sync!", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
            PersistentUI.Instance.ShowDialogBox($"Synced lights to {syncedCount} difficulties!", null, PersistentUI.DialogBoxPresetType.Ok);
        }

        [Exit]
        private void Exit()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
        }
    }
}