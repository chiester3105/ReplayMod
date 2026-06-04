using HarmonyLib;
using ReplayMod.Core;

namespace ReplayMod.Patches.WeaponPatches
{
    [HarmonyPatch(typeof(Laser))]
    public class LaserPatch
    {
        [HarmonyPatch("Fire")]
        [HarmonyPrefix]
        public static bool FirePatch()
        {
            return ReplayManager.i.GetState() != ModStates.Replay;
        }
    }
}
