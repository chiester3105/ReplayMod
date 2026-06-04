using HarmonyLib;
using NuclearOption.SavedMission;

namespace ReplayMod.Debug
{
    [HarmonyPatch(typeof(MissionManager))]
    public class MissionManagerPatch
    {
        [HarmonyPatch("SetMission")]
        [HarmonyPrefix]
        public static void SetMissionPatch(Mission mission, bool checkIfSame)
        {
            Plugin.logger.LogWarning($"checkIfSame: {checkIfSame}\n" +
                $"{mission}");
            foreach(var f in mission.factions)
            {
                Plugin.logger.LogWarning($"{f} {f.factionName}");

                
            }
            
        }
    }
}
