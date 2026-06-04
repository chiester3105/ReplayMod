using HarmonyLib;
using ReplayMod.Core;
using RoadPathfinding;
using UnityEngine;

namespace ReplayMod.Patches.AI_Patches
{
    [HarmonyPatch(typeof(PathfindingAgent))]
    public class PathfinderPatches
    {
        [HarmonyPatch("Pathfind")]
        [HarmonyPrefix]
        public static bool PathFindPrefix(RoadNetwork network, GlobalPosition targetPos, bool stayOnRoad, Transform lineOfSightChecker)
        {
            //Plugin.logger.LogInfo($"pathfind hooked");
            return !(ReplayManager.i.GetState() == ModStates.Replay);
        }

        [HarmonyPatch("GetSteerpoint")]
        [HarmonyPrefix]
        public static bool GetSteerpointPatch(GlobalPosition position, Vector3 forward, float speed, bool stayOnRoad)
        {
            return !(ReplayManager.i.GetState() == ModStates.Replay);
        }
    }
}
