using System.IO;
using UnityEngine;
using ReplayMod.Data;
using ReplayMod.Core;
using NuclearOption.SavedMission;
using Mirage.Serialization;
using System.Collections.Generic;
using System;

namespace ReplayMod.Events.ConcreteEvents
{
    public class SpawnEvent : IReplayEvent
    {
        public EventType EventType { get; }  = EventType.Spawn;
        public double Time { get; set; }
        public uint unitId;
        public GlobalPosition pos;
        public Quaternion rotation;
        public string jsonKey;
        public string factionName;
        public Vector3 startingVelocity;
        public uint ownerId;
        
        public bool isAircraft;
        public LiveryKey.KeyType liveryType;
        public ulong liveryId;
        public int liveryIndex;
        public List<WeaponMount> weapons;

        public void Execute(object worker)
        {
            //MySpawner.Spawn(definitionName, pos, rotation, factionName, unitId);
            if (!(worker is UnitController controller))
            {
                Plugin.logger.LogError("error executing spawn event");
                return;
            }

            Plugin.DebugLog($"Executing spawn: {unitId} {jsonKey}");
            controller.SpawnUnit(this);
            //Plugin.logger.LogInfo($"Spawn executed: {unitId} {jsonKey}");
            
        }
        public void Write(BinaryWriter bw)
        {
            bw.Write(Time);

            bw.Write(unitId);
            bw.Write(jsonKey);
            bw.Write(factionName);

            bw.Write(pos.x);
            bw.Write(pos.y);
            bw.Write(pos.z);
           
            bw.Write(rotation.x);
            bw.Write(rotation.y);
            bw.Write(rotation.z);
            bw.Write(rotation.w);

            bw.Write(startingVelocity.x);
            bw.Write(startingVelocity.y);
            bw.Write(startingVelocity.z);

            bw.Write(isAircraft);
            if(isAircraft)
            {

                bw.Write((byte)liveryType);
                bw.Write(liveryId);
                bw.Write(liveryIndex);

                bw.Write(weapons.Count);
                foreach (var w in weapons) 
                {
                    if (w != null) bw.Write(w.jsonKey);
                    else bw.Write("NULLPTR");
                }
            }
            bw.Write(ownerId);
            //Plugin.logger.LogInfo($"Spawn event written to file: {unitId} {jsonKey}");
        }
        public void Read(BinaryReader br)
        {
            Time = br.ReadDouble();
            
            unitId = (uint)br.ReadInt32();
            jsonKey = br.ReadString();
            factionName = br.ReadString();

            pos = new GlobalPosition 
            {
                x = br.ReadSingle(),
                y = br.ReadSingle(),
                z = br.ReadSingle()
            };

            rotation = new Quaternion()
            {
                x = br.ReadSingle(),
                y = br.ReadSingle(),
                z = br.ReadSingle(),
                w = br.ReadSingle(),
            };

            startingVelocity = new Vector3()
            {
                x = br.ReadSingle(),
                y = br.ReadSingle(),
                z = br.ReadSingle(),
            };

            isAircraft = br.ReadBoolean();

            if (isAircraft)
            {
                liveryType = (LiveryKey.KeyType)br.ReadByte();
                liveryId = br.ReadUInt64();
                liveryIndex = br.ReadInt32();

                int count = br.ReadInt32();
                weapons = new();
                for (int i = 0; i < count; i++)
                {
                    var key = br.ReadString();
                    if (key == "NULLPTR")
                    {
                        weapons.Add(null);
                    }
                    else
                    {
                        var mount = Encyclopedia.WeaponLookup[key];
                        weapons.Add(mount);
                    }
                }
            }
            ownerId = br.ReadUInt32();
        }

        public void Reset()
        {
            unitId = 0;
            pos = default;
            rotation = default;
            jsonKey = null;
            factionName = null;
            startingVelocity = default;
            ownerId = 0;
            isAircraft = false;
            liveryType = default;
            liveryId = 0;
            liveryIndex = 0;
            weapons = null;
        }

        public void CopyFromSnapshot(SpawnSnapshot snapshot)
        {
            Time = snapshot.time;
            this.jsonKey = snapshot.jsonKey;
            this.pos = snapshot.pos;
            this.rotation = snapshot.rotation;
            this.startingVelocity = snapshot.startingVelocity;
            this.factionName = snapshot.factionName;
            this.liveryType = snapshot.liveryType;
            this.liveryIndex = snapshot.liveryIndex;
            this.liveryId = snapshot.liveryId;
            this.weapons = new List<WeaponMount>(snapshot.weapons); 
            this.ownerId = snapshot.ownerId;
        }


        
        

        
    }
}
