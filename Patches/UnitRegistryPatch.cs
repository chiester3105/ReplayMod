using System;
using HarmonyLib;

namespace ReplayMod.Patches
{
    using Player = NuclearOption.Networking.Player;
    [HarmonyPatch(typeof(UnitRegistry))]
    public class UnitRegistryPatch
    {
        public static Action<Unit, PersistentID> onRegisterUnit;
        [HarmonyPatch("RegisterUnit")]
        [HarmonyPostfix]
        public static void RegisterUnitPatch(Unit unit, PersistentID id)
        {
            onRegisterUnit?.Invoke(unit, id);
        }

        public static Action<Unit> onUnregisterUnit;
        [HarmonyPatch("UnregisterUnit")]
        [HarmonyPrefix]
        public static void UnregisterUnitPatch(Unit unit)
        {
            onUnregisterUnit?.Invoke(unit);
        }
    }

    [HarmonyPatch(typeof(UnityEngine.Object))]
    public class UnityPatches
    {
        public static Action<UnityEngine.Object> onObjectInstantiate;
        //[HarmonyPatch("Instantiate")]
        //[HarmonyPrefix]
    }
}
