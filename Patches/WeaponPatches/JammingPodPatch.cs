using HarmonyLib;
using ReplayMod.Core;
namespace ReplayMod.Patches.WeaponPatches
{
    [HarmonyPatch(typeof(JammingPod))]
    public class JammingPodPatch
    {
        [HarmonyPatch("Fire")]
        [HarmonyPrefix]
        public static bool FirePatch()
        {
            return ReplayManager.i.GetState() != ModStates.Replay;
        }
    }
}
