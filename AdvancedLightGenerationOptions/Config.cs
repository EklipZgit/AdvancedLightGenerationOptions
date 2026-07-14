// SPDX-License-Identifier: MIT

using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace AdvancedLightGenerationOptions
{
    public class AdvancedLightGenerationOptionsConfig
    {
        public bool UseWalls { get; set; } = false;
        public float MinWallLength { get; set; } = 1.50f;
        public float AntiFlickerThreshold { get; set; } = 0.25f;
        public float LaserSpeedMulti { get; set; } = 0.8f;
        public int MinLaserSpeed { get; set; } = 1;
        public int MaxLaserSpeed { get; set; } = 8;
        public int MinRotation { get; set; } = -90;
        public int MaxRotation { get; set; } = 90;
        public int MinRotationSpeed { get; set; } = 2;
        public int MaxRotationSpeed { get; set; } = 8;
        public int MinRotationStep { get; set; } = 2;
        public int MaxRotationStep { get; set; } = 8;
        public float MinRotationProp { get; set; } = 0.5f;
        public float MaxRotationProp { get; set; } = 1f;
        public int MinZoomSpeed { get; set; } = 2;
        public int MaxZoomSpeed { get; set; } = 8;
        public int MinZoomStep { get; set; } = 1;
        public int MaxZoomStep { get; set; } = 6;
        public bool WallStrobes { get; set; } = true;
        public bool StrobesCenterOnly { get; set; } = true;
        public bool WallSprinkles { get; set; } = false;
        public int ColorMode { get; set; } = 2; // 0=random per event, 1=alternating, 2=override by bars, 3=override globally
        public float ColorSwitchBeats { get; set; } = 8.0f;
        public bool LaserColorFade { get; set; } = false;
        public float LaserFadeOutLength { get; set; } = 0.5f;
        public bool ResetLongLaserSpeeds { get; set; } = false;
        public bool UseMapIntensityForBrightness { get; set; } = true;
        public float MinBrightness { get; set; } = 0.9f;
        public float MaxBrightness { get; set; } = 1.2f;
        public float RotationInterval { get; set; } = 8.0f;
        public float ZoomInterval { get; set; } = 16.0f;
        public bool DoubleAtIntenseSections { get; set; } = true;
        public int BoostMode { get; set; } = 0; // 0=off, 1=from intensity, 2=periodic, 3=keep existing
        public float BoostPercent { get; set; } = 0.3f;
        public float MinBoostLength { get; set; } = 8.0f;
        public bool RemoveRandomness { get; set; } = false;
        public bool LightBombs { get; set; } = false;
    }

    public static class ConfigManager
    {
        private static readonly string FileName = "advanced_light_generation_options_config.json";
        public static AdvancedLightGenerationOptionsConfig Data { get; private set; } = new AdvancedLightGenerationOptionsConfig();

        private static string GetConfigPath()
        {
            try
            {
                var asmPath = Assembly.GetExecutingAssembly().Location;
                var dir = Path.GetDirectoryName(asmPath);
                return Path.Combine(dir ?? ".", FileName);
            }
            catch
            {
                return FileName;
            }
        }

        public static void Load()
        {
            try
            {
                var path = GetConfigPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var cfg = JsonConvert.DeserializeObject<AdvancedLightGenerationOptionsConfig>(json);
                    if (cfg != null)
                        Data = cfg;
                }
            }
            catch
            {
                // Use default config on error
            }
        }

        public static void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(Data, Formatting.Indented);
                File.WriteAllText(GetConfigPath(), json);
            }
            catch
            {
                // Config save error is non-critical
            }
        }

        public static void Reset()
        {
            Data = new AdvancedLightGenerationOptionsConfig();
            Save();
        }
    }
}