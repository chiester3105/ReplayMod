using System;
using HarmonyLib;

namespace ReplayMod.Patches.WeaponPatches
{
    [HarmonyPatch(typeof(WeaponStation))]
    public class WeaponStationPatch
    {
        public static Action<WeaponStation, Unit, Unit> onWeaponStationFire;
        [HarmonyPatch("LaunchMount")]
        [HarmonyPostfix]
        public static void FirePatch(WeaponStation __instance, Unit owner, Unit target, GlobalPosition aimpoint)
        {
            onWeaponStationFire?.Invoke(__instance, owner, target);
        }
    }
}
