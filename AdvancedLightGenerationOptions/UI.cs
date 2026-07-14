// SPDX-License-Identifier: MIT
// Original work Copyright (c) 2024 Loloppe
// Modified work Copyright (c) 2025 Jonas00000
// Modified work Copyright (c) 2026 EklipZ

using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AdvancedLightGenerationOptions
{
    public class UI
    {
        private GameObject _autolighterMenu;
        private readonly AdvancedLightGenerationOptions _advancedLightGenerationOptions;
        private readonly ExtensionButton _extensionBtn = new ExtensionButton();
        private TMP_InputField _colorSwitchInputField;
        private TMP_InputField _boostPercentInputField;
        private TMP_InputField _minBoostLenInputField;
        private TMP_InputField _minBrightnessInputField;
        private TMP_InputField _maxBrightnessInputField;
        private TMP_InputField _minWallLengthInputField;
        private Toggle _strobesCenterOnlyToggle;
        private Toggle _laserColorFadeToggle;
        private MapEditorUI _mapEditorUI;

        public UI(AdvancedLightGenerationOptions advancedLightGenerationOptions)
        {
            _advancedLightGenerationOptions = advancedLightGenerationOptions;

            PluginLogger.Log("UI constructor called");
            _extensionBtn.Click = () =>
            {
                PluginLogger.Log("Extension button clicked (constructor callback)");
                PersistentUI.Instance.ShowDialogBox("Autolighter button clicked!", null, PersistentUI.DialogBoxPresetType.Ok);
            };

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AdvancedLightGenerationOptions.Icon.png");
            var imageStream = new MemoryStream();
            stream.CopyTo(imageStream);
            byte[] data = imageStream.ToArray();

            Texture2D texture2D = new Texture2D(256, 256);
            texture2D.GetType().GetMethod("LoadImage", new[] { typeof(byte[]) })?.Invoke(texture2D, new object[] { data });

            _extensionBtn.Icon = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height),
                new Vector2(0, 0), 100.0f);
            _extensionBtn.Tooltip = "Advanced Light Generation Options";
            ExtensionButtons.AddButton(_extensionBtn);
            PluginLogger.Log("Extension button added to ExtensionButtons");
        }

        public void ClearMenu()
        {
            if (_autolighterMenu != null)
            {
                UnityEngine.Object.Destroy(_autolighterMenu);
                _autolighterMenu = null;
            }

            var oldMenu = GameObject.Find("Advanced Light Generation Options Menu");
            if (oldMenu != null)
            {
                UnityEngine.Object.Destroy(oldMenu);
            }
        }

        public void AddMenu(MapEditorUI mapEditorUI)
        {
            try
            {
                _mapEditorUI = mapEditorUI;

                if (_autolighterMenu != null)
                {
                    UnityEngine.Object.Destroy(_autolighterMenu);
                }

                var parent = mapEditorUI.MainUIGroup[5].transform;
                PluginLogger.Log($"AddMenu parent={parent.name}, childCount={parent.childCount}");

                // Destroy any stale persistent menu from previous sessions
                var oldMenu = GameObject.Find("Advanced Light Generation Options Menu");
                if (oldMenu != null)
                {
                    UnityEngine.Object.Destroy(oldMenu);
                }

                _autolighterMenu = new GameObject("Advanced Light Generation Options Menu");
                _autolighterMenu.transform.SetParent(parent, false);

                AttachTransform(_autolighterMenu, 460, 340, 1, 1, -150, 0, 1, 1);
                PluginLogger.Log($"Menu transform position={((RectTransform)_autolighterMenu.transform).anchoredPosition}, size={((RectTransform)_autolighterMenu.transform).sizeDelta}");

            Image image = _autolighterMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.24f, 0.24f, 0.24f);

            AddTitle(_autolighterMenu.transform, "Advanced Light Generating Options", new Vector2(-60, -18));
            AddResetButton(_autolighterMenu.transform, "ResetDefaults", new Vector2(145, -18),
                () => { ResetToDefaults(); }, "Reset all settings to default values");
            AddResetButton(_autolighterMenu.transform, "CloseButton", new Vector2(190, -18),
                () => { _autolighterMenu.SetActive(false); }, "Close the Advanced Light Generation Options menu", "X");

            RenderLayoutColumns(_autolighterMenu.transform, CreateLayoutColumns(), -52f, 15f, 8f);

            RenderBottomButtons(_autolighterMenu.transform, CreateBottomButtons(), -322f, 58f);

            _autolighterMenu.SetActive(false);
            _extensionBtn.Click = () => { _autolighterMenu.SetActive(!_autolighterMenu.activeSelf); };
            _extensionBtn.Click = () =>
            {
                PluginLogger.Log("Extension button clicked (AddMenu callback)");
                _autolighterMenu.SetActive(!_autolighterMenu.activeSelf);
            };
            PluginLogger.Log("AddMenu complete, click callback assigned");
        }
        catch (Exception ex)
        {
            PluginLogger.Log($"AddMenu exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private LayoutButton[] CreateBottomButtons()
    {
        return new[]
        {
            new LayoutButton("GenLight", "Regenerate all lights", () => { _advancedLightGenerationOptions.Light(); }, "Generate lighting events based on the current map.", true),
            new LayoutButton("SyncDiffs", "Sync to All Diffs", () => { _advancedLightGenerationOptions.SyncToAllDiffs(); }, "Sync the lighting events to all other difficulties in the map.", false),
            new LayoutButton("RegenSelectedLanes", "Regen Selected Lanes", () => { /* TODO: Implement lane-specific regeneration */ }, "Regenerate lighting events for selected lanes only.", false),
            new LayoutButton("RegenSelectedTime", "Regen Selected Time", () => { /* TODO: Implement time-range regeneration */ }, "Regenerate lighting events for selected time range.", false),
        };
    }

    private void RenderBottomButtons(Transform parent, LayoutButton[] buttons, float y, float spacing)
    {
        var x = -200f;
        foreach (var button in buttons)
        {
            var btn = AddButton(parent, button.Id, button.Text, new Vector2(x, y), button.OnClick, button.Tooltip);
            if (button.AddColorFade)
            {
                var colorFade = btn.gameObject.AddComponent<ColorFadeAnimation>();
                colorFade.image = btn.Button.GetComponent<Image>();
            }
            x += spacing;
        }
    }

    // ================================================================================
    // DECLARATIVE UI LAYOUT SYSTEM
    // ================================================================================
    // This section defines the UI using a declarative three-column layout model.
    //
    // STRUCTURE:
    // - LayoutColumn: Represents a vertical column at a specific X position.
    // - LayoutSection: Groups rows under a header within a column.
    // - LayoutRow: Encapsulates rendering logic for a single control row.
    //
    // ADDING NEW CONTROLS:
    // 1. Choose the appropriate row factory based on the control type:
    //    - ToggleRow(id, label, getValue, setValue, tooltip, capture?, interactable?)
    //    - IntRow(id, label, getValue, setValue, tooltip, capture?, interactable?)
    //    - FloatRow(id, label, getValue, setValue, tooltip, capture?, interactable?)
    //    - IntRangeRow(id, label, getMin, getMax, setMin, setMax, tooltip)
    //    - FloatRangeRow(id, label, getMin, getMax, setMin, setMax, tooltip)
    //
    // 2. Add the row to the appropriate section's rows array.
    // 3. If the row needs state capture (for inter-control dependencies), use the
    //    capture parameter to store a reference to the control.
    // 4. If the row should be conditionally interactable, use the interactable
    //    parameter to provide a predicate function.
    //
    // ADDING NEW SECTIONS:
    // Create a new LayoutSection with a unique ID, display header text, and an
    // array of rows, then add it to the appropriate column's sections array.
    //
    // SPACING:
    // - Row spacing: 15 units (controlled by RenderLayoutColumns)
    // - Section spacing: 8 units (controlled by RenderLayoutColumns)
    //
    // IMPORTANT: Keep this documentation in sync with any changes to the layout
    // system, row factory signatures, or rendering behavior.
    //
    // BOTTOM BUTTONS:
    // The bottom action buttons are defined declaratively as an array that renders
    // from left to right. Use CreateBottomButtons() to define the button array.
    // Each button has: id, text, onClick handler, and optional tooltip.
    // Buttons are automatically positioned starting from the left corner with
    // consistent spacing.
    // ================================================================================

    private LayoutColumn[] CreateLayoutColumns()
    {
        return new[]
        {
            // Column 1: Brightness
            new LayoutColumn(-190f, new[]
            {
                new LayoutSection("Brightness", "Brightness", new[]
                {
                    ToggleRow("UseBrightness", "Dynamic", () => ConfigManager.Data.UseMapIntensityForBrightness, value =>
                    {
                        ConfigManager.Data.UseMapIntensityForBrightness = value;
                        if (_minBrightnessInputField != null) _minBrightnessInputField.interactable = value;
                        if (_maxBrightnessInputField != null) _maxBrightnessInputField.interactable = value;
                    }, "Brightness based on map intensity."),
                    FloatRow("MinBright", "Min Brightness", () => ConfigManager.Data.MinBrightness,
                        value => ConfigManager.Data.MinBrightness = value, "Min brightness", input => _minBrightnessInputField = input,
                        () => ConfigManager.Data.UseMapIntensityForBrightness),
                    FloatRow("MaxBright", "Max Brightness", () => ConfigManager.Data.MaxBrightness,
                        value => ConfigManager.Data.MaxBrightness = value, "Max brightness", input => _maxBrightnessInputField = input,
                        () => ConfigManager.Data.UseMapIntensityForBrightness),
                }),
                new LayoutSection("Colors", "Colors", new[]
                {
                    IntRow("ColorMode", "Color Mode", () => ConfigManager.Data.ColorMode, value =>
                    {
                        ConfigManager.Data.ColorMode = Mathf.Clamp(value, 0, 3);
                        if (_colorSwitchInputField != null) _colorSwitchInputField.interactable = ConfigManager.Data.ColorMode == 2;
                        if (_laserColorFadeToggle != null) _laserColorFadeToggle.interactable = ConfigManager.Data.ColorMode != 2 && ConfigManager.Data.ColorMode != 3;
                    }, "0: Random, 1: Alternating, 2: Switch every X beats, 3: Switch all lights"),
                    FloatRow("ColorSwitch", "Color Switch", () => ConfigManager.Data.ColorSwitchBeats,
                        value => ConfigManager.Data.ColorSwitchBeats = value, "Beats between color switches.", input => _colorSwitchInputField = input,
                        () => ConfigManager.Data.ColorMode == 2),
                    ToggleRow("ColorLaserFade", "Laser Color Fade", () => ConfigManager.Data.LaserColorFade,
                        value => ConfigManager.Data.LaserColorFade = value, "Colors change during long lasers.",
                        toggle => _laserColorFadeToggle = toggle, () => ConfigManager.Data.ColorMode != 2 && ConfigManager.Data.ColorMode != 3),
                }),
                new LayoutSection("Boost", "Boost", new[]
                {
                    IntRow("BoostMode", "Boost Mode", () => ConfigManager.Data.BoostMode, value =>
                    {
                        ConfigManager.Data.BoostMode = Mathf.Clamp(value, 0, 3);
                        if (_boostPercentInputField != null) _boostPercentInputField.interactable = ConfigManager.Data.BoostMode == 1;
                        if (_minBoostLenInputField != null) _minBoostLenInputField.interactable = ConfigManager.Data.BoostMode == 1 || ConfigManager.Data.BoostMode == 2;
                    }, "Boost mode."),
                    FloatRow("MinBoostLen", "Boost Length", () => ConfigManager.Data.MinBoostLength,
                        value => ConfigManager.Data.MinBoostLength = value, "Boost section length or interval.", input => _minBoostLenInputField = input,
                        () => ConfigManager.Data.BoostMode == 1 || ConfigManager.Data.BoostMode == 2),
                    FloatRow("BoostPercent", "Boost Amount", () => ConfigManager.Data.BoostPercent,
                        value => ConfigManager.Data.BoostPercent = value, "Boost amount.", input => _boostPercentInputField = input,
                        () => ConfigManager.Data.BoostMode == 1),
                }),
            }),
            // Column 2: Lasers
            new LayoutColumn(-50f, new[]
            {
                new LayoutSection("Lasers", "Lasers", new[]
                {
                    IntRangeRow("LaserSpeed", "Speed Range", () => ConfigManager.Data.MinLaserSpeed, () => ConfigManager.Data.MaxLaserSpeed,
                        value => ConfigManager.Data.MinLaserSpeed = value, value => ConfigManager.Data.MaxLaserSpeed = value, "Laser speed range"),
                    ToggleRow("LaserColorFade", "Color Fade", () => ConfigManager.Data.LaserColorFade,
                        value => ConfigManager.Data.LaserColorFade = value, "Colors change during long lasers.", toggle => _laserColorFadeToggle = toggle),
                    ToggleRow("ResetLaserSpeeds", "Reset Speeds", () => ConfigManager.Data.ResetLongLaserSpeeds,
                        value => ConfigManager.Data.ResetLongLaserSpeeds = value, "Reset laser speeds on every event."),
                    FloatRow("LaserFade", "Fade Out", () => ConfigManager.Data.LaserFadeOutLength,
                        value => ConfigManager.Data.LaserFadeOutLength = value, "Laser fade out length"),
                }),
                new LayoutSection("Ring", "Ring Rotation", new[]
                {
                    FloatRow("RotInt", "Interval in Beats", () => ConfigManager.Data.RotationInterval,
                        value => ConfigManager.Data.RotationInterval = value, "Rotation interval"),
                    IntRangeRow("Rotation", "Rot Range", () => ConfigManager.Data.MinRotation, () => ConfigManager.Data.MaxRotation,
                        value => ConfigManager.Data.MinRotation = value, value => ConfigManager.Data.MaxRotation = value, "Rotation range"),
                    IntRangeRow("RotationSpeed", "Speed Range", () => ConfigManager.Data.MinRotationSpeed, () => ConfigManager.Data.MaxRotationSpeed,
                        value => ConfigManager.Data.MinRotationSpeed = value, value => ConfigManager.Data.MaxRotationSpeed = value, "Rotation speed range"),
                    IntRangeRow("RotationStep", "Step Range", () => ConfigManager.Data.MinRotationStep, () => ConfigManager.Data.MaxRotationStep,
                        value => ConfigManager.Data.MinRotationStep = value, value => ConfigManager.Data.MaxRotationStep = value, "Rotation step range"),
                    FloatRangeRow("RotationProp", "Prop Range", () => ConfigManager.Data.MinRotationProp, () => ConfigManager.Data.MaxRotationProp,
                        value => ConfigManager.Data.MinRotationProp = value, value => ConfigManager.Data.MaxRotationProp = value, "Rotation prop range"),
                }),
                new LayoutSection("RingZoom", "Ring Zoom", new[]
                {
                    FloatRow("ZoomInt", "Interval in Beats", () => ConfigManager.Data.ZoomInterval,
                        value => ConfigManager.Data.ZoomInterval = value, "Zoom interval"),
                    IntRangeRow("ZoomSpeed", "Speed Range", () => ConfigManager.Data.MinZoomSpeed, () => ConfigManager.Data.MaxZoomSpeed,
                        value => ConfigManager.Data.MinZoomSpeed = value, value => ConfigManager.Data.MaxZoomSpeed = value, "Zoom speed range"),
                    IntRangeRow("ZoomStep", "Step Range", () => ConfigManager.Data.MinZoomStep, () => ConfigManager.Data.MaxZoomStep,
                        value => ConfigManager.Data.MinZoomStep = value, value => ConfigManager.Data.MaxZoomStep = value, "Zoom step range"),
                    ToggleRow("DoubleAtIntense", "Dynamic Ring Events", () => ConfigManager.Data.DoubleAtIntenseSections,
                        value => ConfigManager.Data.DoubleAtIntenseSections = value, "Double ring events during intense sections."),
                }),
            }),
            // Column 3: General
            new LayoutColumn(110f, new[]
            {
                new LayoutSection("General", "General", new[]
                {
                    FloatRow("AntiFlicker", "Anti Flicker", () => ConfigManager.Data.AntiFlickerThreshold,
                        value => ConfigManager.Data.AntiFlickerThreshold = value, "Minimum space between light events."),
                    ToggleRow("RemoveRandomness", "No Randomness", () => ConfigManager.Data.RemoveRandomness,
                        value => ConfigManager.Data.RemoveRandomness = value, "Remove randomness."),
                    ToggleRow("LightBombs", "Light Bombs", () => ConfigManager.Data.LightBombs,
                        value => ConfigManager.Data.LightBombs = value, "Generate lights for bombs."),
                }),
                new LayoutSection("Walls", "Walls", new[]
                {
                    ToggleRow("UseWalls", "Use Long Walls", () => ConfigManager.Data.UseWalls, value =>
                    {
                        ConfigManager.Data.UseWalls = value;
                        if (_minWallLengthInputField != null) _minWallLengthInputField.interactable = value;
                    }, "Use long walls."),
                    FloatRow("MinWallLen", "Min Wall Length", () => ConfigManager.Data.MinWallLength,
                        value => ConfigManager.Data.MinWallLength = value, "Minimum wall length.", input => _minWallLengthInputField = input,
                        () => ConfigManager.Data.UseWalls),
                    ToggleRow("WallStrobes", "Wall Strobes", () => ConfigManager.Data.WallStrobes, value =>
                    {
                        ConfigManager.Data.WallStrobes = value;
                        if (_strobesCenterOnlyToggle != null) _strobesCenterOnlyToggle.interactable = value;
                    }, "Generate wall strobes."),
                    ToggleRow("CenterStrobes", "Strobe Center", () => ConfigManager.Data.StrobesCenterOnly,
                        value => ConfigManager.Data.StrobesCenterOnly = value, "Center wall strobes.", toggle => _strobesCenterOnlyToggle = toggle,
                        () => ConfigManager.Data.WallStrobes),
                    ToggleRow("WallSprinkles", "Wall Sprinkles", () => ConfigManager.Data.WallSprinkles,
                        value => ConfigManager.Data.WallSprinkles = value, "Generate wall sprinkles."),
                }),
            }),
        };
    }

    private LayoutRow IntRow(string id, string label, Func<int> getValue, Action<int> setValue, string tooltip,
        Action<TMP_InputField> capture = null, Func<bool> interactable = null)
    {
        return new LayoutRow((parent, position) =>
        {
            var input = AddTextInput(parent, id, label, position, getValue().ToString(), (value, inputField) =>
            {
                if (!int.TryParse(value, out var parsed)) return;
                setValue(parsed);
                ConfigManager.Save();
            }, false, tooltip);
            capture?.Invoke(input);
            if (interactable != null) input.interactable = interactable();
        });
    }

    private LayoutRow FloatRow(string id, string label, Func<float> getValue, Action<float> setValue, string tooltip,
        Action<TMP_InputField> capture = null, Func<bool> interactable = null)
    {
        return new LayoutRow((parent, position) =>
        {
            var input = AddTextInput(parent, id, label, position, getValue().ToString("0.00"), (value, inputField) =>
            {
                if (!float.TryParse(value, out var parsed)) return;
                setValue(parsed);
                ConfigManager.Save();
            }, false, tooltip);
            capture?.Invoke(input);
            if (interactable != null) input.interactable = interactable();
        });
    }

    private LayoutRow ToggleRow(string id, string label, Func<bool> getValue, Action<bool> setValue, string tooltip,
        Action<Toggle> capture = null, Func<bool> interactable = null)
    {
        return new LayoutRow((parent, position) =>
        {
            var toggle = AddCheckbox(parent, id, label, position, getValue(), value =>
            {
                setValue(value);
                ConfigManager.Save();
            }, false, tooltip);
            capture?.Invoke(toggle);
            if (interactable != null) toggle.interactable = interactable();
        });
    }

    private LayoutRow IntRangeRow(string id, string label, Func<int> getMin, Func<int> getMax,
        Action<int> setMin, Action<int> setMax, string tooltip)
    {
        return new LayoutRow((parent, position) => AddRangeInputs(parent, id, label, position,
            getMin().ToString(), getMax().ToString(),
            value =>
            {
                if (!int.TryParse(value, out var parsed)) return;
                setMin(parsed);
                ConfigManager.Save();
            },
            value =>
            {
                if (!int.TryParse(value, out var parsed)) return;
                setMax(parsed);
                ConfigManager.Save();
            }, tooltip));
    }

    private LayoutRow FloatRangeRow(string id, string label, Func<float> getMin, Func<float> getMax,
        Action<float> setMin, Action<float> setMax, string tooltip)
    {
        return new LayoutRow((parent, position) => AddRangeInputs(parent, id, label, position,
            getMin().ToString("0.00"), getMax().ToString("0.00"),
            value =>
            {
                if (!float.TryParse(value, out var parsed)) return;
                setMin(parsed);
                ConfigManager.Save();
            },
            value =>
            {
                if (!float.TryParse(value, out var parsed)) return;
                setMax(parsed);
                ConfigManager.Save();
            }, tooltip));
    }

    private void RenderLayoutColumns(Transform parent, LayoutColumn[] columns, float startY, float rowSpacing, float sectionSpacing)
    {
        foreach (var column in columns)
        {
            var y = startY;
            foreach (var section in column.Sections)
            {
                AddHeader(parent, section.Id + "Header", section.Header, new Vector2(column.X, y));
                y -= rowSpacing;
                foreach (var row in section.Rows)
                {
                    row.Render(parent, new Vector2(column.X, y));
                    y -= rowSpacing;
                }

                y -= sectionSpacing;
            }
        }
    }

    private void AddTitle(Transform parent, string text, Vector2 pos)
    {
        var entryLabel = UnityEngine.Object.Instantiate(PersistentUI.Instance.ButtonPrefab.Text.gameObject, parent);
        entryLabel.name = "Advanced Light Generating Options Label";
        var rectTransform = (RectTransform)entryLabel.transform;
        MoveTransform(rectTransform, 250f, 20f, 0.5f, 1f, pos.x, pos.y);
        var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();
        textComponent.name = "Advanced Light Generating Options";
        textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
        textComponent.fontMaterial = PersistentUI.Instance.ButtonPrefab.Text.fontMaterial;
        textComponent.color = PersistentUI.Instance.ButtonPrefab.Text.color;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.enableAutoSizing = false;
        textComponent.raycastTarget = false;
        textComponent.fontSize = 14;
        textComponent.text = text;
        textComponent.enabled = true;
    }

    private void AddHeader(Transform parent, string title, string text, Vector2 pos)
    {
        var entryLabel = UnityEngine.Object.Instantiate(PersistentUI.Instance.ButtonPrefab.Text.gameObject, parent);
        entryLabel.name = title + " Label";
        var rectTransform = (RectTransform)entryLabel.transform;
        MoveTransform(rectTransform, 125f, 18f, 0.5f, 1f, pos.x - 15f, pos.y, 0f, 0.5f);
        var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();
        textComponent.name = title;
        textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
        textComponent.fontMaterial = PersistentUI.Instance.ButtonPrefab.Text.fontMaterial;
        textComponent.color = PersistentUI.Instance.ButtonPrefab.Text.color;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.enableAutoSizing = false;
        textComponent.raycastTarget = false;
        textComponent.fontSize = 13;
        textComponent.text = text;
        textComponent.enabled = true;
    }

    private void AddRangeInputs(Transform parent, string title, string label, Vector2 pos, string minValue, string maxValue,
        UnityAction<string> onMinChanged, UnityAction<string> onMaxChanged, string tooltip)
    {
        AddTextInput(parent, title + "Min", string.Empty, pos, minValue,
            (value, inputField) => onMinChanged(value), false, tooltip);
        AddLabel(parent, title + "Dash", "-", new Vector2(pos.x + 19f, pos.y), new Vector2(8f, 14f));
        AddTextInput(parent, title + "Max", string.Empty, new Vector2(pos.x + 38f, pos.y), maxValue,
            (value, inputField) => onMaxChanged(value), false, tooltip);
        AddLabel(parent, title + "RangeLabel", label, new Vector2(pos.x + 98f, pos.y), new Vector2(80f, 14f), TextAlignmentOptions.Left);
    }

    private UIButton AddButton(Transform parent, string title, string text, Vector2 pos, UnityAction onClick,
            string tooltip = "")
        {
            var button = UnityEngine.Object.Instantiate(PersistentUI.Instance.ButtonPrefab, parent);
            MoveTransform(button.transform, 100, 28, 0.5f, 1, pos.x, pos.y);
            PluginLogger.Log($"AddButton {title}: pos={pos}, parent={parent.name}");

            button.name = title;
            button.Button.onClick.AddListener(onClick);

            button.SetText(text);
            button.Text.enableAutoSizing = false;
            button.Text.fontSize = 10;

            if (!string.IsNullOrEmpty(tooltip))
            {
                AddTooltip(button.gameObject, tooltip);
            }

            return button;
        }

        private void AddResetButton(Transform parent, string title, Vector2 pos, UnityAction onClick,
            string tooltip = "", string text = "Reset")
        {
            var button = UnityEngine.Object.Instantiate(PersistentUI.Instance.ButtonPrefab, parent);
            MoveTransform(button.transform, 40, 18, 0.5f, 1, pos.x, pos.y);

            button.name = title;
            button.Button.onClick.AddListener(onClick);

            button.SetText(text);
            button.Text.enableAutoSizing = false;
            button.Text.fontSize = 10;
            button.Text.alignment = TextAlignmentOptions.Center;

            if (string.IsNullOrEmpty(tooltip)) return;
            AddTooltip(button.gameObject, tooltip);
        }

        private void AddLabel(Transform parent, string title, string text, Vector2 pos, Vector2? size = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var entryLabel = UnityEngine.Object.Instantiate(PersistentUI.Instance.ButtonPrefab.Text.gameObject, parent);
            entryLabel.name = title + " Label";
            var rectTransform = ((RectTransform)entryLabel.transform);

            var labelSize = size ?? new Vector2(200f, 18f);
            MoveTransform(rectTransform, labelSize.x, labelSize.y, 0.5f, 1, pos.x, pos.y);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.fontMaterial = PersistentUI.Instance.ButtonPrefab.Text.fontMaterial;
            textComponent.color = PersistentUI.Instance.ButtonPrefab.Text.color;
            textComponent.alignment = alignment;
            textComponent.enableAutoSizing = false;
            textComponent.raycastTarget = false;
            textComponent.fontSize = 9;
            textComponent.text = text;
            textComponent.enabled = true;
            PluginLogger.Log($"AddLabel {title}: text='{text}', pos={pos}, font={(textComponent.font != null ? textComponent.font.name : "null")}, material={(textComponent.fontMaterial != null ? textComponent.fontMaterial.name : "null")}");
        }

        private TMP_InputField AddTextInput(Transform parent, string title, string text, Vector2 pos, string value,
            UnityAction<string, TMP_InputField> onChange, bool labelLeft = true, string tooltip = "")
        {
            var entryLabel = UnityEngine.Object.Instantiate(PersistentUI.Instance.ButtonPrefab.Text.gameObject, parent);
            entryLabel.name = title + " Label";
            var rectTransform = ((RectTransform)entryLabel.transform);

            if (labelLeft) MoveTransform(rectTransform, 90, 16, 0.5f, 1, pos.x - 65f, pos.y);
            else MoveTransform(rectTransform, 90, 16, 0.5f, 1, pos.x + 64f, pos.y);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.fontMaterial = PersistentUI.Instance.ButtonPrefab.Text.fontMaterial;
            textComponent.color = PersistentUI.Instance.ButtonPrefab.Text.color;
            textComponent.alignment = labelLeft ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            textComponent.enableAutoSizing = false;
            textComponent.raycastTarget = false;
            textComponent.fontSize = 9;
            textComponent.text = text;
            textComponent.enabled = true;
            PluginLogger.Log($"AddTextInput label {title}: text='{text}', pos={pos}, font={(textComponent.font != null ? textComponent.font.name : "null")}, material={(textComponent.fontMaterial != null ? textComponent.fontMaterial.name : "null")}");

            var textInput = UnityEngine.Object.Instantiate(PersistentUI.Instance.TextInputPrefab, parent);
            MoveTransform(textInput.transform, 30, 14, 0.5f, 1, pos.x, pos.y);
            textInput.GetComponent<Image>().pixelsPerUnitMultiplier = 3;
            textInput.InputField.text = value;
            textInput.InputField.onFocusSelectAll = false;
            textInput.InputField.textComponent.alignment = TextAlignmentOptions.Left;
            textInput.InputField.textComponent.enableAutoSizing = false;
            textInput.InputField.textComponent.fontSize = 10;
            textInput.InputField.textComponent.margin = new Vector4(1f, 0f, 1f, 0f);
            textInput.InputField.textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;
            if (textInput.InputField.textViewport != null)
            {
                textInput.InputField.textViewport.offsetMin = new Vector2(2f, 0f);
                textInput.InputField.textViewport.offsetMax = new Vector2(-2f, 0f);
            }

            textInput.InputField.onValueChanged.AddListener((val) => onChange(val, textInput.InputField));

            if (!string.IsNullOrEmpty(tooltip))
            {
                AddTooltip(textInput.gameObject, tooltip);
                AddTooltip(entryLabel, tooltip);
            }

            return textInput.InputField;
        }

        private Toggle AddCheckbox(Transform parent, string title, string text, Vector2 pos, bool value,
            UnityAction<bool> onClick, bool labelLeft = false, string tooltip = "")
        {
            var original = GameObject.Find("Strobe Generator").GetComponentInChildren<Toggle>(true);
            var toggleObject = UnityEngine.Object.Instantiate(original, parent.transform);
            MoveTransform(toggleObject.transform, 16, 16, 0.5f, 1, pos.x, pos.y);

            var toggleComponent = toggleObject.GetComponent<Toggle>();
            var colorBlock = toggleComponent.colors;
            colorBlock.normalColor = Color.white;
            toggleComponent.colors = colorBlock;
            toggleComponent.isOn = value;
            toggleComponent.onValueChanged.AddListener(onClick);

            var entryLabel = UnityEngine.Object.Instantiate(PersistentUI.Instance.ButtonPrefab.Text.gameObject, parent);
            entryLabel.name = title + " Label";
            var rectTransform = ((RectTransform)entryLabel.transform);
            if (labelLeft) MoveTransform(rectTransform, 100, 14, 0.5f, 1, pos.x - 60f, pos.y);
            else MoveTransform(rectTransform, 100, 14, 0.5f, 1, pos.x + 62f, pos.y);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.fontMaterial = PersistentUI.Instance.ButtonPrefab.Text.fontMaterial;
            textComponent.color = PersistentUI.Instance.ButtonPrefab.Text.color;
            textComponent.alignment = labelLeft ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            textComponent.enableAutoSizing = false;
            textComponent.raycastTarget = false;
            textComponent.fontSize = 9;
            textComponent.text = text;
            textComponent.enabled = true;
            PluginLogger.Log($"AddCheckbox label {title}: text='{text}', pos={pos}, font={(textComponent.font != null ? textComponent.font.name : "null")}, material={(textComponent.fontMaterial != null ? textComponent.fontMaterial.name : "null")}");

            if (!string.IsNullOrEmpty(tooltip))
            {
                AddTooltip(toggleObject.gameObject, tooltip);
                AddTooltip(entryLabel, tooltip);
            }

            return toggleComponent;
        }

        private void AttachTransform(GameObject obj, float sizeX, float sizeY, float anchorX, float anchorY,
            float anchorPosX, float anchorPosY, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            RectTransform rectTransform = obj.AddComponent<RectTransform>();
            rectTransform.localScale = new Vector3(1, 1, 1);
            rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            rectTransform.pivot = new Vector2(pivotX, pivotY);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(anchorX, anchorY);
            rectTransform.anchoredPosition = new Vector3(anchorPosX, anchorPosY, 0);
        }

        private void MoveTransform(Transform transform, float sizeX, float sizeY, float anchorX, float anchorY,
            float anchorPosX, float anchorPosY, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            if (!(transform is RectTransform rectTransform)) return;

            rectTransform.localScale = new Vector3(1, 1, 1);
            rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            rectTransform.pivot = new Vector2(pivotX, pivotY);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(anchorX, anchorY);
            rectTransform.anchoredPosition = new Vector3(anchorPosX, anchorPosY, 0);
        }

        private void AddTooltip(GameObject obj, string tooltipText)
        {
            var tooltip = obj.AddComponent<Tooltip>();
            tooltip.TooltipOverride = tooltipText;
            tooltip.AppearDelay = 0.2f;
        }

        private void ResetToDefaults()
        {
            ConfigManager.Reset();

            UnityEngine.Object.Destroy(_autolighterMenu);
            AddMenu(_mapEditorUI);
            _autolighterMenu.SetActive(true);
        }
    }

    public class ColorFadeAnimation : MonoBehaviour
    {
        public Image image;
        private float _time;
        private readonly Color _pink = new Color(0.671f, 0.094f, 0.259f);
        private readonly Color _blue = new Color(0.094f, 0.416f, 0.678f);
        private const float FadeSpeed = 0.25f;

        private void Update()
        {
            if (!image) return;

            _time += Time.deltaTime * FadeSpeed;
            float t = (Mathf.Sin(_time) + 1f) / 2f; // between 0 and 1
            image.color = Color.Lerp(_pink, _blue, t);
        }
    }
}