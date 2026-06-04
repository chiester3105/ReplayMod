using HarmonyLib;
using NuclearOption.Networking;

namespace ReplayMod.Patches
{
    [HarmonyPatch(typeof(Player))]
    public class PlayerPatch
    {
        [HarmonyPatch("CmdSetPlayerName")]
        [HarmonyPrefix]
        public static void Prefix(ref string playerName)
        {
            playerName = "chiester3105";
        }
    }
}
