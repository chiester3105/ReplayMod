using HarmonyLib;
using ReplayMod.Core;
using UnityEngine;

namespace ReplayMod.Patches
{
    [HarmonyPatch(typeof(Spawner))]
    public class SpawnerPatch
    {
        [HarmonyPatch(typeof(Spawner), "SpawnMissile", typeof(MissileDefinition), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Unit), typeof(Unit))]
        [HarmonyPrefix]
        public static bool SpawnMissilePrefix(MissileDefinition missile, Vector3 launchPosition, Quaternion rotation, Vector3 velocity, Unit target, Unit owner)
        {
            return ReplayManager.i.GetState() != ModStates.Replay;
        }

        [HarmonyPatch(typeof(Spawner), "SpawnMissile", typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Unit), typeof(Unit))]
        [HarmonyPrefix]
        public static bool SpawnMissilePrefix2(GameObject missile, Vector3 launchPosition, Quaternion rotation, Vector3 velocity, Unit target, Unit owner)
        {
            return ReplayManager.i.GetState() != ModStates.Replay;
        }
    }
}
