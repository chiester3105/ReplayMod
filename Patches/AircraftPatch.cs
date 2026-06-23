using System;
using HarmonyLib;
using ReplayMod.Core;

namespace ReplayMod.Patches
{
    [HarmonyPatch(typeof(Aircraft))]
    public class AircraftPatch
    {
        [HarmonyPatch("StartEjectionSequence")]
        [HarmonyPrefix]
        public static bool StartEjectionSequencePatch()
        {
            if (ReplayManager.i.GetState() == ModStates.Replay)
            { 
                return false;
            }
            return true;
        }

        public static Action<Aircraft, bool> onSetGear;
        [HarmonyPatch("SetGear", new Type[] { typeof(bool) })]
        [HarmonyPrefix]
        public static void SetGearPatch(Aircraft __instance, bool deployed)
        {
            onSetGear?.Invoke(__instance, deployed);
        }


        [HarmonyPatch("CmdLaunchMissile")]
        [HarmonyPrefix]
        public static bool CmdLaunchMissilePatch(byte stationIndex, Unit target, GlobalPosition aimpoint)
        {
            return ReplayManager.i.GetState() != ModStates.Replay;
        }

        [HarmonyPatch("RpcLaunchMissile")]
        [HarmonyPrefix]
        public static bool RpcLaunchMissilePatch(byte stationIndex, Unit target, GlobalPosition aimpoint)
        {
            
            return ReplayManager.i.GetState() != ModStates.Replay;
        }
    }
}
