using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System;
using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System.Collections.Concurrent;
using ReplayMod.Core;
using ReplayMod.Debug;
using System.Security.Cryptography;

namespace ReplayMod
{
    public static class PluginInfo
    {
        public const string GUID = "com.chiester3105.replayMod";
        public const string Name = "ReplayMod";
        public const string Version = "1.0.0";
    }
    
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource logger;
        public static Plugin Instance { get; private set; }
        public static ReplayManager ReplayManager { get; private set; }

        //version info
        public static byte Major { get; private set; }
        public static byte Minor { get; private set; }
        public static byte Patch { get; private set; }
        public const int AppId = 0x3105;

        public void Awake()
        {
            try
            {
                gameObject.AddComponent<ReplayManager>();
                DontDestroyOnLoad(gameObject);

                Plugin.logger = base.Logger;
                Plugin.logger.LogInfo("Plugin Awake started");

                var harmony = new Harmony("ReplayMod");
                harmony.PatchAll();
                Plugin.logger.LogInfo("Harmony patches applied");
                LoadVersionInfo();
                Plugin.logger.LogInfo("Plugin loaded");
                Task.Run(() => EncyclopediaLookup());
                
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Plugin failed to load: {e}");
            }
        }

        public void Start()
        {
            ReflectionCache.Init();
        }
        public async Task EncyclopediaLookup()
        {
            await Task.Delay(11000); // idk if there a load finish callback
            await UniTask.SwitchToMainThread();
            try
            {
                foreach (var kvp in Encyclopedia.Lookup)
                {
                    /*
                    Plugin.logger.LogInfo($"KEY: {kvp.Key} " +
                        $"| defUnitName {kvp.Value.unitName}" +
                        $"| jsonKey {kvp.Value.jsonKey}" +
                        $"| bogeyName {kvp.Value.bogeyName}");
                    */
                    if (kvp.Key != kvp.Value.jsonKey)
                    {
                        Plugin.logger.LogError($"key {kvp.Key}, jsonKey {kvp.Value.jsonKey}");
                    }
                }
                foreach(var kvp in Encyclopedia.WeaponLookup)
                {
                    Plugin.logger.LogInfo($"{kvp.Key}: {kvp.Value.jsonKey}");
                }
            }
            catch (Exception e)
            {
                Plugin.logger.LogError(e);
            }
        }

        const int maxActionsPerFrame = 10;
        public void Update() 
        {
            int iterations = 0;
            while(iterations++ < maxActionsPerFrame && _executionQueue.TryDequeue(out var action))
            {
                action.Invoke();
            }
            
            
            if(Input.GetKeyDown(KeyCode.L))
            {
                DebugTools.DisableRbToSelectedAircraft();
            }
            if(Input.GetKeyDown(KeyCode.X))
            {
                DebugTools.DetachRandomPart();
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                DebugTools.ShowWeaponStations();
            }
        }  
       
        public void FixedUpdate()
        {

        }

        private static ConcurrentQueue<Action> _executionQueue = new();
        public static void SafeLog(string message)
        {
            _executionQueue.Enqueue(() => logger.LogInfo(message));
        }
        public static void SafeLogError(string message)
        {
            _executionQueue.Enqueue(() => logger.LogError(message));
        }
        public static void SafeLogWarning(string message)
        {
            _executionQueue.Enqueue(() => logger.LogWarning(message));
        }
        private void LoadVersionInfo()
        {
            string[] args = PluginInfo.Version.Split('.');
            Major = byte.Parse(args[0]);
            Minor = byte.Parse(args[1]);
            Patch = byte.Parse(args[2]);
        }

    }
}
