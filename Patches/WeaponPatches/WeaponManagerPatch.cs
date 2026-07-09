using System;
using HarmonyLib;
namespace ReplayMod.Patches.WeaponPatches
{
    [HarmonyPatch(typeof(WeaponManager))]
    public class WeaponManagerPatch
    {
        public static Action<WeaponManager, byte> onSetActiveStation;
        [HarmonyPatch(nameof(WeaponManager.SetActiveStation))]
        [HarmonyPostfix]
        private static void SetActiveStationPatch(WeaponManager __instance, byte stationIndex)
        {
            onSetActiveStation?.Invoke(__instance, stationIndex);
        }

        public static Action<WeaponManager> onWeaponManagerFire;
        [HarmonyPatch(nameof(WeaponManager.Fire))]
        [HarmonyPostfix]
        private static void FirePatch(WeaponManager __instance)
        {
            onWeaponManagerFire?.Invoke(__instance);
        }
    }
}
