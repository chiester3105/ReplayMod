using HarmonyLib;
using NuclearOption.SavedMission;
namespace ReplayMod.Debug
{
    [HarmonyPatch(typeof(SinglePlayerMenu))]
    public class SinglePlayerButton
    {
        [HarmonyPatch("StartMission")]
        [HarmonyPrefix]
        public static void Patch(Mission mission)
        {
            Plugin.logger.LogWarning($"{mission}");
            foreach (var f in FactionRegistry.factions)
            {
                Plugin.logger.LogInfo($"{f}, {f.factionName}");
            }
            foreach (var f in mission.factions)
            {
                Plugin.logger.LogWarning($"{f} {f.factionName}");


            }
        }
    }
}
