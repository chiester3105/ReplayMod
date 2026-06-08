using HarmonyLib;
using ReplayMod.Core;

namespace ReplayMod.Patches
{
    [HarmonyPatch(typeof(Building))]
    public class BuildingPatch
    {
        [HarmonyPatch("UnitDisabled")]
        [HarmonyPrefix]
        public static bool UnitDisabledPrefix(Building __instance, bool oldState, bool newState)
        {
            return ReplayManager.i.ShouldContinue(__instance.persistentID.Id);
        }
        [HarmonyPatch("Collapse")]
        [HarmonyPrefix]
        public static bool CollapsePatch(Building __instance)
        {
            return ReplayManager.i.ShouldContinue(__instance.persistentID.Id);
        }
    }
}
