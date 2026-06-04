using System.IO;
using ReplayMod.Core;

namespace ReplayMod.Events.ConcreteEvents
{
    public class DespawnEvent : IReplayEvent
    {
        public EventType EventType { get; } = EventType.Despawn;
        public double Time { get ; set; }
        public uint unitId;
        public void Execute(object worker)
        {
            if(worker is UnitController controller)
            {
                controller.DespawnUnit(unitId);
            }
        }

        public void Read(BinaryReader br)
        {
            Time = br.ReadDouble();
            unitId = br.ReadUInt32();
        }

        public void Reset()
        {
            
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(Time);
            bw.Write(unitId);
        }
    }
}
