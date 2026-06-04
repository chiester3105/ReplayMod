using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ReplayMod.Events.ConcreteEvents;
using UnityEngine;

namespace ReplayMod.Data
{
    public struct SpawnSnapshot
    {
        public double time;
        public uint unitId;
        public string jsonKey;
        public GlobalPosition pos;
        public Quaternion rotation;
        public Vector3 startingVelocity;
        public string factionName;
        public LiveryKey.KeyType liveryType;
        public int liveryIndex;
        public ulong liveryId;
        public List<WeaponMount> weapons;
        public uint ownerId;

        public static SpawnSnapshot Create(SpawnEvent se)
        {
            return new SpawnSnapshot
            {
                unitId = se.unitId,
                jsonKey = se.jsonKey,
                pos = se.pos,
                rotation = se.rotation,
                startingVelocity = se.startingVelocity,
                factionName = se.factionName,
                liveryType = se.liveryType,
                liveryIndex = se.liveryIndex,
                liveryId = se.liveryId,
                weapons = se.weapons != null ? new List<WeaponMount>(se.weapons) : new List<WeaponMount>(),
                ownerId = se.ownerId
            };
        }
    }
}
