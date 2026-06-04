using HarmonyLib;
using ReplayMod.Core;

namespace ReplayMod.Patches.WeaponPatches
{
    [HarmonyPatch(typeof(Gun))]
    public class GunPatch
    {
        [HarmonyPatch("Fire")]
        [HarmonyPrefix]
        public static bool FirePatch()
        {
            return ReplayManager.i.GetState() != ModStates.Replay;
        }
    }
}
