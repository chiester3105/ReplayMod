using NuclearOption.Jobs;
using ReplayMod.Core;
using HarmonyLib;
namespace ReplayMod.Patches
{
    //[HarmonyPatch(typeof(GroundVehicle))]
    public class GroundVehiclePatches
    {
        //[HarmonyPatch("Awake")]
        //[HarmonyPrefix]
        public static bool AwakePatch()
        {
            return !(ReplayManager.i.GetState()==ModStates.Replay);
        }

        //[HarmonyPatch("UpdateJobFields_Pathfinder")]
        //[HarmonyPrefix]
        public static bool UpdateJobFields_Pathfinder()
        {
            return !(ReplayManager.i.GetState() == ModStates.Replay);
        }
        //[HarmonyPatch("UpdateJobFields_Obstacles")]
        //[HarmonyPrefix]
        public static bool UpdateJobFields_Obstacles()
        {
            return !(ReplayManager.i.GetState() == ModStates.Replay);
        }

        //[HarmonyPatch("UnitDisabled")]
        //[HarmonyPrefix]
        public static bool UnitDisabledPrefix(GroundVehicle __instance, bool oldState, bool newState)
        {
            return ReplayManager.i.ShouldContinue(__instance.persistentID.Id);
        }
        //[HarmonyPatch("WreckAndRemove")]
        //[HarmonyPrefix]
        public static bool WreckAngRemovePatch(GroundVehicle __instance)
        {
            return ReplayManager.i.ShouldContinue(__instance.persistentID.Id);
        }
        //[HarmonyPatch("SpawnWreckage")]
        //[HarmonyPrefix]
        public static bool SpawnWreckage()
        {
            return !(ReplayManager.i.GetState() == ModStates.Replay);
        }
    }

    //[HarmonyPatch(typeof(GroundVehicle), nameof(GroundVehicle.OnDestroy))]
    public static class GVPatch2
    {
        //[HarmonyPrefix]
        public static bool OnDestroyPrefix(GroundVehicle __instance)
        {
            Plugin.logger.LogInfo("GV ondestroy hooked");
            if (ReplayManager.i.GetState() == ModStates.Replay)
            {
                ReflectionCache.baseUnitOnDestroyDelegate.Invoke(__instance);
                JobManager.Remove(ref __instance.JobPart);
            }
            return true;
        }
    }
}
