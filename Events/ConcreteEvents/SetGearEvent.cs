using System.IO;
using ReplayMod.Core;

namespace ReplayMod.Events.ConcreteEvents
{
    public class SetGearEvent : IReplayEvent
    {
        public double Time { get ; set; }

        public EventType EventType { get; } = EventType.ToggleGear;
        public uint unitId;
        public bool deployed;
        public void Execute(object worker = null)
        {
            if (worker is UnitController u) u.SetGear(this);
        }

        public void Read(BinaryReader br)
        {
            Time = br.ReadDouble();
            unitId = br.ReadUInt32();
            deployed = br.ReadBoolean();
        }

        public void Reset()
        {
            
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(Time);
            bw.Write(unitId);
            bw.Write(deployed);
        }
    }
}
