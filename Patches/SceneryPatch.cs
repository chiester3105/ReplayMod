using HarmonyLib;
using ReplayMod.Core;

namespace ReplayMod.Patches
{
    //[HarmonyPatch(typeof(Scenery))]
    public class SceneryPatch
    {
        //[HarmonyPatch("Collapse")]
        //[HarmonyPrefix]
        public static bool CollapsePatch(Scenery __instance)
        {
            return ReplayManager.i.ShouldContinue(__instance.persistentID.Id);
        }
    }
}
