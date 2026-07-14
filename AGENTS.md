# AGENTS.md

## Project

This repository contains `AdvancedLightGenerationOptions`, a ChroMapper plugin for generating Beat Saber light events from map structure. The plugin is C# targeting .NET Framework 4.8 and references assemblies from a local ChroMapper install through `Directory.Build.props.user`.

## Build

Create `Directory.Build.props.user` in the repo root when building locally:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <ChroMapperPath>YOUR_PATH_TO_CHROMAPPER</ChroMapperPath>
  </PropertyGroup>
</Project>
```

Then run:

```powershell
dotnet build AutoLighterV2.sln
```

The output DLL is under `AdvancedLightGenerationOptions/bin/Debug/` or `AdvancedLightGenerationOptions/bin/Release/`.

## Architecture Notes

- `AdvancedLightGenerationOptions/AdvancedLightGenerationOptions.cs` is the plugin entry point. It hooks scene load, finds ChroMapper grid containers, wires UI, generates events, and syncs events to other difficulties.
- `AdvancedLightGenerationOptions/EventGenerator.cs` contains the generation algorithm. Keep generation logic mostly pure: accept `MapEditorState` plus `AdvancedLightGenerationOptionsConfig`, return `List<BaseEvent>`.
- `AdvancedLightGenerationOptions/UI.cs` builds the current ChroMapper extension-button menu by instantiating ChroMapper UI prefabs.
- `AdvancedLightGenerationOptions/Config.cs` stores plugin settings beside the built plugin DLL.

## ChroMapper Integration Rules

- Prefer ChroMapper native models over raw JSON when running inside the editor: `BaseEvent`, `BaseNote`, `BaseObstacle`, `BaseSlider`, and their `CustomData` / `CustomColor` / `CustomLightID` properties.
- When mutating existing map objects, preserve undo/redo by using ChroMapper action types where practical, especially `BeatmapObjectModifiedAction`, `BeatmapObjectModifiedCollectionAction`, `BeatmapObjectPlacementAction`, and `ActionCollectionAction`.
- After spawning/deleting events through `EventGridContainer`, call the appropriate post-workflow and refresh methods, usually `DoPostObjectsSpawnedWorkflow()`, `DoPostObjectsDeleteWorkflow()`, and `RefreshPool(true)`.
- For custom data, use version-aware properties where available:
  - `BaseObject.CustomKeyColor`
  - `BaseEvent.CustomKeyLightID`
  - `BaseEvent.CustomKeyEasing`
  - `BaseEvent.CustomKeyLerpType`
  - `BaseEvent.CustomKeyLightGradient`
- Always call `RefreshCustom()` or route through `CustomData`/typed properties after editing custom data so parsed fields like `CustomColor` and `CustomLightID` stay correct.

## Editing Guidance

- Do not rewrite unrelated plugin structure while adding advanced tools.
- Keep existing `Light` behavior stable unless explicitly changing it.
- New preservation/fill/split/lock operations should be implemented as separate services first, then wired into UI.
- Avoid direct deletion of user-authored events unless the operation is explicitly destructive and clearly named.
- Keep map-version differences visible in helper methods rather than scattering `_color` vs `color` string checks.

## Research Notes

The initial research plan for advanced lighting work is in `docs/advanced-autolights-research.md`.
Use [ChroMapper PLUGINS.md](https://github.com/Caeden117/ChroMapper/blob/master/PLUGINS.md) as the baseline guide for plugin entry points, managed assembly references, CMUI dialogs, extension buttons, and requirement checks.
