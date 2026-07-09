using System.IO;
using ReplayMod.Core;

namespace ReplayMod.Events.ConcreteEvents
{
    [ReplayEvent(EventType.SetWeaponStation)]
    public class SetActiveStationEvent : IReplayEvent
    {
        public double Time { get; set ; }

        public EventType EventType { get; } = EventType.SetWeaponStation;

        public uint unitId;
        public byte stationIdx;
        public void Execute(object worker = null)
        {
            if (worker is UnitController controller)
                controller.Execute(this);
        }
        public void Read(BinaryReader br)
        {
            Time = br.ReadDouble();
            stationIdx = br.ReadByte();
            unitId = br.ReadUInt32();
        }

        public void Reset()
        {
            Time = 0;
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(Time);
            bw.Write(stationIdx);
            bw.Write(unitId);
        }
    }
}
