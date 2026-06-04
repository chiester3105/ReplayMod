using HarmonyLib;
using ReplayMod.Core;

namespace ReplayMod.Patches
{
    [HarmonyPatch(typeof(UnitCommand))]
    public class UnitCommandPatches
    {
        [HarmonyPatch("SetDestination")]
        [HarmonyPrefix]
        public static bool SetDestinationPatch(GlobalPosition waypoint, bool playerCommand)
        {
            if (ReplayManager.i.GetState() == ModStates.Replay)
                return false;
            return true;
        }
    }
}
