using HarmonyLib;
using ReplayMod.Core;
namespace ReplayMod.Patches
{
    [HarmonyPatch(typeof(GameplayUI))]
    public class GameplayUIPatch
    {
        [HarmonyPatch("ShowJoinMenu")]
        [HarmonyPrefix]
        public static bool ShowJoinMenu()
        {
            return ReplayManager.i.GetState() != ModStates.Replay;
        }
    }
}
