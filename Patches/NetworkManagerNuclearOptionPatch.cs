using System;
using HarmonyLib;
using NuclearOption.Networking;
using NuclearOption.SceneLoading;
namespace ReplayMod.Patches
{
    [HarmonyPatch(typeof(NetworkManagerNuclearOption))]
    public class NetworkManagerNuclearOptionPatch
    {
        [HarmonyPatch("ClientLoadScene")]
        [HarmonyPrefix]
        public static void ClientLoad(LoadMapMessage msg)
        {
            Plugin.logger.LogWarning($"ClientLoadScene, {msg.Key.TypeName}");
        }

        [HarmonyPatch("ServerLoadMapScene")]
        [HarmonyPrefix]
        public static void ServerLoad(MapKey mapKey, IProgress<float> progress)
        {
            Plugin.logger.LogWarning($"ServerLoadMapScene, {mapKey.TypeName} | {mapKey.Path} | {mapKey.Type} ");
        }
    }
}
