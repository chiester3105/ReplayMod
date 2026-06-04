using HarmonyLib;
using ReplayMod.Core;

namespace ReplayMod.Patches.AI_Patches
{
    [HarmonyPatch(typeof(AIHeloLandingState))]
    public class AIHeloTakeoffStatePatch
    {
        [HarmonyPatch("UpdateState")]
        [HarmonyPrefix]
        public static bool UpdateStatePatch(Pilot pilot)
        {
            if (ReplayManager.i.GetState() == ModStates.Replay)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch("FixedUpdateState")]
        [HarmonyPrefix]
        public static bool FixedUpdateState(Pilot pilot)
        {
            if (ReplayManager.i.GetState() == ModStates.Replay)
            {
                return false;
            }
            return true;
        }
    }
}
