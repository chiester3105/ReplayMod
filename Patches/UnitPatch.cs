using System;
using HarmonyLib;
using UnityEngine;

namespace ReplayMod.Patches
{
    [HarmonyPatch(typeof(Unit))]
    public class UnitPatch
    {
        public static Action<Unit, byte> onPartDetached;
        [HarmonyPatch("DetachPart")]
        [HarmonyPrefix]
        public static void DetachPartPatch(Unit __instance, byte partID, Vector3 velocity, Vector3 relativePos)
        {
            onPartDetached?.Invoke(__instance, partID);
        }
    }
}
