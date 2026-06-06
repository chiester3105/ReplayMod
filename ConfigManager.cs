using BepInEx.Configuration;
using UnityEngine;

namespace ReplayMod
{
    public static class ConfigManager
    {
        public static ConfigEntry<KeyboardShortcut> ToggleMenu { get; set; }
        public static ConfigEntry<KeyboardShortcut> ToggleRecord { get; set; }
        public static ConfigEntry<KeyboardShortcut> TogglePause { get; set; }
        public static ConfigEntry<KeyboardShortcut> CreateCameraWaypoint { get; set; }
        public static ConfigEntry<KeyboardShortcut> DeleteLastCameraWaypoint { get; set; }

        public static void Configure(ConfigFile config)
        {
            ToggleMenu = config.Bind("Hotkeys", "Toggle menu", new KeyboardShortcut(UnityEngine.KeyCode.F5));
            ToggleRecord = config.Bind("Hotkeys", "Toggle record", new KeyboardShortcut(UnityEngine.KeyCode.I));
            TogglePause = config.Bind("Hotkeys", "Toggle pause", new KeyboardShortcut(UnityEngine.KeyCode.Space));
            CreateCameraWaypoint = config.Bind("Hotkeys", "Add camera waypoint", new KeyboardShortcut(UnityEngine.KeyCode.F6));
            DeleteLastCameraWaypoint = config.Bind("Hotkeys", "Delete last camera waypoint", new KeyboardShortcut(UnityEngine.KeyCode.F7));
        }

        
    }
    public static class KeyboardShortcutExtensions
    {
        public static bool GetDown(this KeyboardShortcut key)
        {
            if (Input.GetKeyDown(key.MainKey))
            {
                return true;
            }
            return false;
        }
    }
}
