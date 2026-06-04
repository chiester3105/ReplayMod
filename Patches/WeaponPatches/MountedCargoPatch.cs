using HarmonyLib;
using ReplayMod.Core;

namespace ReplayMod.Patches.WeaponPatches
{
    [HarmonyPatch(typeof(MountedCargo))]
    public class MountedCargoPatch
    {
        [HarmonyPatch("Fire")]
        [HarmonyPrefix]
        public static bool FirePatch()
        {
            return ReplayManager.i.GetState() != ModStates.Replay;
        }
    }
}
