# Advanced Light Generation Options Research And Plan

Date: 2026-06-26

## Source Corpus

Primary sources inspected:

- [Caeden117/ChroMapper](https://github.com/Caeden117/ChroMapper)
- [ChroMapper PLUGINS.md](https://github.com/Caeden117/ChroMapper/blob/master/PLUGINS.md)
- [FallenCharlotte/ChroMapper-PropEdit](https://github.com/FallenCharlotte/ChroMapper-PropEdit)
- [FallenCharlotte/ChroMapper-ExtendedLightIDs](https://github.com/FallenCharlotte/ChroMapper-ExtendedLightIDs)
- [KivalEvan/ChroMapper-Selector](https://github.com/KivalEvan/ChroMapper-Selector)
- [KivalEvan/ChroMapper-ReWorkflow](https://github.com/KivalEvan/ChroMapper-ReWorkflow)
- [KivalEvan/ChroMapper-Lolighter](https://github.com/KivalEvan/ChroMapper-Lolighter)
- [LightAi39/ChroMapper-AutoModder](https://github.com/LightAi39/ChroMapper-AutoModder)
- [Vainstains/ChroMapperPluginTemplate](https://github.com/Vainstains/ChroMapperPluginTemplate)
- [Loloppe/ChroMapper-AutoMapper](https://github.com/Loloppe/ChroMapper-AutoMapper)
- [rynan4818/ChroMapper-ColorPresetManager](https://github.com/rynan4818/ChroMapper-ColorPresetManager)
- [Swifter1243/ReMapper](https://github.com/Swifter1243/ReMapper)

ReMapper is not a ChroMapper runtime plugin. It is a TypeScript map-generation library over raw Beat Saber/Heck schemas. It is still useful as a typed vocabulary reference for lighting actions, Chroma custom data, light ID transforms, custom events, environment objects, and iterator-style batch operations.

The ChroMapper plugin guide is the baseline for plugin-shaped work: ChroMapper loads plugin DLLs from the `Plugins` folder, plugin projects should target .NET Framework, and typical plugin references come from `ChroMapper_Data/Managed`, especially `Main.dll`, Unity assemblies, `Unity.InputSystem.dll`, and `Plugins.dll`. For UI work, the guide points to CMUI dialog components and right-side `ExtensionButtons`. It also documents custom `RequirementCheck` registration, which matters when a plugin-generated map should suggest or require Chroma.

## Current Plugin Baseline

`AdvancedLightGenerationOptions.Light()` currently:

- Requires notes to exist.
- Reads notes, obstacles, arcs/sliders, existing boost events, and bookmarks into `MapEditorState`.
- Deletes every event whose type is not `14`, `15`, or `100`.
- Calls `EventGenerator.GenerateAll`.
- Spawns returned `BaseEvent`s into `EventGridContainer`.
- Updates lighter metadata on the difficulty.

That means current `Light` is intentionally destructive for regular light lanes. The new work should preserve that behavior and add separate non-destructive operations.

## Chroma Custom Data Knowledge Base

ChroMapper uses `BaseObject.CustomData` as the shared storage surface and parses common Chroma fields into typed properties.

Version-aware event keys:

| Concept | v2 key | v3/v4 key | ChroMapper API |
| --- | --- | --- | --- |
| Color | `_color` | `color` | `BaseObject.CustomColor`, `BaseObject.CustomKeyColor` |
| Light ID | `_lightID` | `lightID` | `BaseEvent.CustomLightID`, `BaseEvent.CustomKeyLightID` |
| Prop ID | `_propID` | `propID` | `BaseEvent.CustomPropID`, `BaseEvent.CustomKeyPropID` |
| Easing | `_easing` | `easing` | `BaseEvent.CustomEasing`, `BaseEvent.CustomKeyEasing` |
| Lerp type | `_lerpType` | `lerpType` | `BaseEvent.CustomLerpType`, `BaseEvent.CustomKeyLerpType` |
| Light gradient | `_lightGradient` | `lightGradient` | `BaseEvent.CustomLightGradient`, `BaseEvent.CustomKeyLightGradient` |
| Name filter | `_nameFilter` | `nameFilter` | `BaseEvent.CustomNameFilter`, `BaseEvent.CustomKeyNameFilter` |

ChroMapper converts v2 and v3 custom-data names internally. Prefer the typed/key properties above inside the plugin.

Chroma light value/action mapping:

| Value | Meaning |
| --- | --- |
| `0` | off |
| `1` | blue on |
| `2` | blue flash |
| `3` | blue fade |
| `4` | blue transition |
| `5` | red on |
| `6` | red flash |
| `7` | red fade |
| `8` | red transition |
| `9` | white on |
| `10` | white flash |
| `11` | white fade |
| `12` | white transition |

Transition/fade behavior:

- `On` establishes a steady base color.
- `Flash` creates a flash shader offset in the editor preview.
- `Fade` creates fade shader behavior.
- `Transition` renders as a transition from the previous linked light event, using the current event's color and optional easing/lerp data.
- ChroMapper links `Prev`/`Next` light events by event type, and when advanced Chroma light ID transition support is enabled it links by the first `CustomLightID`.

Legacy Chroma RGB-int values:

- ChroMapper still recognizes event values at or above `ColourManager.RgbintOffset` (`2000000000`) as encoded RGB values.
- Prefer explicit custom-data color arrays for new work because ChroMapper's modern Chroma parsing, PropEdit, and ReMapper all model `_color` / `color`.

## Light IDs And Environment Lanes

ChroMapper's key environment/light ID surface:

- `EventGridContainer.platformDescriptor.LightingManagers` contains per-event-type lighting managers for the loaded environment.
- `CreateEventTypeLabels` maps between editor lanes, prop groups, and real light IDs:
  - `PropIdToLightIds(type, propID)`
  - `PropIdToLightIdsJ(type, propID)`
  - `LightIdsToPropId(type, int[] lightID)`
  - `EditorToLightID(type, lightID)`
  - `LightIDToEditor(type, lightID)`
- `EventGridContainer.PropagationEditing` can be `Off`, `Prop`, or `Light`.
- In propagation mode lane `0` represents all lights; lanes after that represent prop groups or light IDs depending on mode.

`ExtendedLightIDs` is a practical reference:

- It listens for platform/map load.
- It reads environment-enhancement light IDs from `BaseEnvironmentEnhancement.Components["ILightWithId"]["lightID"]`.
- It reads event `CustomLightID`s from `EventGridContainer.AllLightEvents`.
- It updates each lighting manager's `LightIDPlacementMap` and reverse map.
- It creates placeholder off events with `CustomLightID = new[] { id }` so ChroMapper exposes additional light ID lanes.

Implementation implication: for `Split to light ids`, use ChroMapper's environment mappings first. If an event has no `CustomLightID`, treat it as targeting all lights in that event type. Resolve all real IDs from the active `LightingManager.LightIDPlacementMap.Values`, optionally augmented by ExtendedLightIDs-style event/environment IDs.

## Global And Explicit Color Resolution

ChroMapper preview color resolution for light events is effectively:

1. If event value is legacy RGB-int, decode it.
2. If event is off, use off color.
3. If event value is blue/red/white, choose the corresponding regular or boost color.
4. If `Settings.Instance.EmulateChromaLite` and `CustomColor` exists, custom color overrides non-white light values.
5. Brightness is applied through `FloatValue` for visible light events.

Boost state is determined from the last type-5 event at or before the event time. ChroMapper's `EventAppearanceSO.SetEventAppearance` receives a `boost` boolean from `AllBoostEvents.FindLast(x => x.JsonTime <= obj.JsonTime)?.Value == 1`.

Global color sources:

- Runtime platform colors: `LoadInitialMap.Platform.Colors`.
- Difficulty overrides: `BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorLeft`, `CustomEnvColorRight`, `CustomEnvColorWhite`, and boost variants.
- Note/wall overrides: `CustomColorLeft`, `CustomColorRight`, `CustomColorObstacle`.
- Fallback platform defaults: `LoadInitialMap.Platform.DefaultColors`.

For notes, bombs, arcs/chains, and walls, ChroMapper also stores explicit colors via `BaseObject.CustomColor` and `CustomKeyColor`.

## Selection And Undo/Redo Patterns

Selector uses:

- `SelectionController.SelectedObjects`
- `SelectionController.Select(obj, true, false, false)`
- `SelectionController.Deselect(obj, false)`
- `SelectionController.SelectionChangedEvent?.Invoke()`

PropEdit shows the safer mutation path:

- Clone original objects with `BeatmapFactory.Clone`.
- For property-only edits, build modified clones and use `BeatmapObjectModifiedCollectionAction`.
- For time or conflict-sensitive edits, delete/spawn through the owning collection and record `BeatmapObjectModifiedAction`s.
- Refresh selection and pools after applying batch edits.

For our operations:

- Lock-color operations should be undoable modified-collection actions.
- Split-light-ID operations should be an action collection containing deletion of originals and placement of per-ID clones.
- AutoFillLights should use placement actions for generated events and should avoid modifying existing events.

## ReMapper Concepts Worth Borrowing

ReMapper's `LightEvent` wrapper gives a useful API shape:

- `lightID` can be a number or array.
- `chromaColor` maps to v2 `_color` or v3 `color`.
- `easing` maps to `_easing` / `easing`.
- `lerpType` maps to `_lerpType` / `lerpType`.
- Helpers expose `on`, `flash`, `fade`, `transition`, and `off` as semantic methods over event values.

ReMapper's `LightIterator` is a good model for internal tooling:

- Filter events by ID presence or ID range.
- Normalize ID values to arrays for processing.
- Set, append, initialize, shift, normalize, or remap IDs.
- Simplify single-ID arrays back to a single ID where desired.

For this C# plugin, do not embed ReMapper. Instead, build a small `ChromaEventAdapter` / `LightIdSet` utility layer that provides similar normalization over `BaseEvent`.

## Feature Plan

### 1. Foundation: Chroma Utility Layer

Create focused helpers before UI work:

- `ChromaColorUtils`
  - Resolve map version keys via existing `CustomKeyColor`.
  - Convert `UnityEngine.Color` to/from `SimpleJSON.JSONArray`.
  - Detect explicit color on `BaseObject`.
  - Set explicit color and refresh parsed custom fields.
- `LightEventUtils`
  - `IsGeneratedLightEvent(BaseEvent)` / `IsUserLightEvent(BaseEvent)` if we add metadata.
  - Normalize `CustomLightID` to an ID set.
  - Determine whether an event targets all lights.
  - Resolve effective color from value, boost state, global colors, `CustomColor`, legacy RGB-int, and `FloatValue`.
- `EnvironmentLightIdResolver`
  - Read available IDs for each event type from `EventGridContainer.platformDescriptor.LightingManagers`.
  - Include IDs from existing event `CustomLightID`s and v3 environment enhancements.
  - Resolve all IDs targeted by an event.
- `UndoableMapEdits`
  - Common action helpers for placement, deletion, replacement, and modified collections.

### 2. AutoFillLights

Add a new button beside `Generate Lighting` named `AutoFillLights`.

Behavior:

- Generate candidate events using the same `EventGenerator.GenerateAll(state, cfg)`.
- Do not delete existing events.
- Build an occupancy index from current light events.
- For each generated candidate, determine affected event type and affected light IDs.
- Keep the candidate only when it does not conflict with existing lighting in the target lane/ID at that time.

Recommended first version of gap detection:

- Treat an event type with no `CustomLightID` as all lights for that type.
- Treat a candidate with no `CustomLightID` as all lights for that type.
- Use a time epsilon matching ChroMapper conflict behavior.
- Consider a lane occupied if an existing event of the same type and overlapping light target exists within epsilon.

More advanced version:

- Convert existing event streams into active intervals by event type plus light ID.
- Only fill spans where no active non-off state exists.
- Preserve transitions by not inserting generated starts inside existing transition chains.

### 3. Lock Rendered Colors

Add operations to write explicit colors onto objects that do not already have explicit colors.

Buttons/UI:

- `Lock Visible Colors`
- Options/toggles for lights, notes, bombs, walls, arcs/chains.
- Optional scope: selected objects only vs entire difficulty.

Rules:

- Skip objects that already have explicit `CustomColor`.
- Lights:
  - Resolve boost state at event time.
  - Resolve global red/blue/white or boost red/blue/white.
  - Apply `FloatValue` for rendered brightness if the user's intent is exact editor-rendered color; consider a toggle for "include brightness".
  - Do not override white events with custom Chroma color because ChroMapper preview treats white as overriding Chroma custom colors.
  - Do not lock off events unless an explicit option is added.
- Notes and bombs:
  - Red/blue notes use current note colors.
  - Bomb color policy needs product decision: likely use obstacle color or a separate bomb color if available in current ChroMapper appearance code.
- Walls:
  - Use current obstacle color.
- Arcs/chains:
  - Use their note color side and current note colors.

Implementation detail:

- Use `BaseObject.CustomColor = color`, then `WriteCustom()` or write to `GetOrCreateCustom()[CustomKeyColor]` and `RefreshCustom()`.
- Wrap changes in undoable modified-collection actions.

### 4. Split To Light IDs

Add a selected-event operation:

- Read selected `BaseEvent`s from `SelectionController.SelectedObjects.OfType<BaseEvent>()`.
- Filter to light events.
- Resolve targeted IDs:
  - If `CustomLightID` exists, split that set.
  - If no `CustomLightID`, expand to every ID known for the event type in the loaded environment.
  - If only prop ID exists, use `CreateEventTypeLabels.PropIdToLightIds(type, propID)`.
- Replace the original with one clone per target ID.
- Each clone gets `CustomLightID = new[] { id }`.
- Preserve `JsonTime`, `Type`, `Value`, `FloatValue`, `CustomColor`, easing, lerp type, gradients, and other custom data.
- Do not duplicate if the event is already a single-ID event unless "force resplit" is added.

Risk:

- A light gradient or transition chain split only makes sense if subsequent events are split consistently. The first implementation should warn/confirm if selected events include transition values without their linked neighbors.

### 5. Effect Porting

Add an internal model for copying lighting effects:

- `LightEffectClip`
  - Beat offset from clip start.
  - Event type.
  - Value and float value.
  - Normalized light IDs.
  - Custom color, easing, lerp, gradient, custom payload.
- Capture from selected events or time range.
- Paste/port with:
  - Time offset.
  - Event type remap.
  - Light ID remap.
  - Color strategy: preserve explicit colors, convert to current global colors, or force explicit colors.

This model will also help AutoFillLights because generated candidates can be compared as normalized clips.

### 6. UI Direction

The current one-panel UI is already dense. Add a separate `Advanced AutoLights` extension button/modal rather than trying to keep extending the existing panel.

Suggested tabs:

- `Generate`: current settings plus Autolight and AutoFillLights.
- `Preserve`: gap-fill options and protected lane/type/ID controls.
- `Colors`: lock rendered colors and scope toggles.
- `Light IDs`: split selected events, extend detected IDs, ID remapping.
- `Clips`: future copy/port lighting effect tools.

Keep the existing `Advanced Light Generation Options` menu operational for current users. The advanced modal can reuse current config values but should not force a full redesign in the first feature pass.

## Implementation Order

1. Add utility layer and unit-testable logic where possible.
2. Add `AutoFillLights` with simple conflict/gap semantics.
3. Add lock rendered colors for light events only.
4. Extend lock rendered colors to notes, bombs, walls, arcs, and chains.
5. Add selected-event `Split to light ids`.
6. Add advanced UI shell/tabs and move new actions there.
7. Add effect clip capture/paste/porting.

## Open Questions

- Should AutoFillLights treat existing off events as occupied or as available for generated lighting after that point?
- Should lock colors include `FloatValue` brightness or write full global color and preserve brightness separately?
- For bombs, should explicit color follow obstacle color, note color, or current ChroMapper bomb appearance?
- Should split-to-ID expand all-light events by prop group first or every physical light ID by default?
- Should generated AdvancedLightGenerationOptions events carry plugin metadata so future fills can distinguish generated events from hand-authored lighting?
