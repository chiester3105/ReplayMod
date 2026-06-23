using HarmonyLib;
using NuclearOption.Jobs;
using ReplayMod.Core;

namespace ReplayMod.Patches
{
    //[HarmonyPatch(typeof(Ship))]
    public class ShipPatches
    {
        //[HarmonyPatch("UnitDisabled")]
        //[HarmonyPrefix]
        public static bool UnitDisabledPrefix(Ship __instance, bool oldState, bool newState)
        {
            return ReplayManager.i.ShouldContinue(__instance.persistentID.Id);
        }
    }
}
