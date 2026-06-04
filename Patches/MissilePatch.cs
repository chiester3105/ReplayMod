using System;
using HarmonyLib;

namespace ReplayMod.Patches
{
    [HarmonyPatch(typeof(Missile))]
    public class MissilePatch
    {
        public static Action<Missile> onDetonate;
        [HarmonyPatch("Detonate")]
        [HarmonyPrefix]
        public static void DetonatePatch(Missile __instance)
        {
            onDetonate?.Invoke(__instance);
        }
    }
}
