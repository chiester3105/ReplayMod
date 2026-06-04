using System;
using HarmonyLib;
using NuclearOption.Networking;
using ReplayMod.Core;
using UnityEngine;

namespace ReplayMod.Patches
{
    [HarmonyPatch(typeof(Unit))]
    public class UnitPatches
    {
        public static Action<Unit, byte> onPartDetached;
        [HarmonyPatch("DetachPart")]
        [HarmonyPrefix]
        public static void DetachPartPatch(Unit __instance, byte partID, Vector3 velocity, Vector3 relativePos)
        {
            onPartDetached?.Invoke(__instance, partID);
        }

        //[HarmonyPatch("OnDestroy")]
        //[HarmonyPrefix]
        public static bool OnDestroyPrefix(Unit __instance)
        {
            return ReplayManager.i.ShouldContinue(__instance.persistentID.Id);
        }
    }
}
