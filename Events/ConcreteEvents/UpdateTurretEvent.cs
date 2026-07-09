using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReplayMod.Core;

namespace ReplayMod.Events.ConcreteEvents
{
    [ReplayEvent(EventType.UpdateTurret)]
    public class UpdateTurretEvent : IReplayEvent
    {
        public double Time { get ; set ; }

        public EventType EventType { get; } = EventType.UpdateTurret;

        public float elevationAngle;
        public float traverseAngle;
        public uint attachedUnitId;
        public byte weaponStationIdx;
        public byte turretIdx;
        public void Execute(object worker = null)
        {
            if (worker is UnitController u)
            {
                u.Execute(this); 
            }
        }

        public void Read(BinaryReader br)
        {
            elevationAngle = br.ReadSingle();
            traverseAngle = br.ReadSingle();
            attachedUnitId = br.ReadUInt32();
            weaponStationIdx = br.ReadByte();
            turretIdx = br.ReadByte();
            Time = br.ReadDouble();
        }

        public void Reset()
        {
            elevationAngle = default;
            traverseAngle = default;
            attachedUnitId = default;
            weaponStationIdx = default;
            turretIdx = default;
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(elevationAngle);
            bw.Write(traverseAngle);
            bw.Write(attachedUnitId);
            bw.Write(weaponStationIdx);
            bw.Write(turretIdx);
            bw.Write(Time);
        }
    }
}
