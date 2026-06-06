using System;
using HarmonyLib;


namespace ReplayMod.Patches
{
    [HarmonyPatch(typeof(Datum))]
    public class DatumPatch
    {
        public static Action onShiftOrigin;
        [HarmonyPatch("AfterOriginShift")]
        [HarmonyPostfix]
        public static void Postfix()
        {
            onShiftOrigin?.Invoke();
        }
    }
}
