using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReplayMod.Core;

namespace ReplayMod.Events.ConcreteEvents
{
    public class WeaponFireEvent : IReplayEvent
    {
        public double Time { get; set; } 

        public EventType EventType { get; } = EventType.WeaponFire;

        public uint unitId;
        public byte stationIdx;
        public byte weaponIdx;

        public void Execute(object worker = null)
        {
            Plugin.DebugLog("Executing fire event");
            if (worker is UnitController u) u.ExecuteFire(this); 
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(Time);
            bw.Write(unitId);
            bw.Write(stationIdx);
            bw.Write(weaponIdx);
        }

        public void Read(BinaryReader br)
        {
            Time = br.ReadDouble();
            unitId = br.ReadUInt32();
            stationIdx = br.ReadByte();
            weaponIdx = br.ReadByte();
        }

        public void Reset()
        {
            Time = 0;
            unitId = 0;
            stationIdx = 0;
            weaponIdx = 0;
        }

    }
}
