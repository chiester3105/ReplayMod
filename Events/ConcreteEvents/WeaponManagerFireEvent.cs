using System.IO;
using ReplayMod.Core;

namespace ReplayMod.Events.ConcreteEvents
{
    [ReplayEvent(EventType.WeaponManagerFire)]
    public class WeaponManagerFireEvent : IReplayEvent
    {
        public double Time { get ; set; }

        public EventType EventType { get; } = EventType.WeaponManagerFire;
        public uint unitId;

        public void Execute(object worker = null)
        {
            if (worker is UnitController controller)
                controller.WeaponManagerFire(this);
        }

        public void Read(BinaryReader br)
        {
            Time = br.ReadDouble();
            unitId = br.ReadUInt32();
        }

        public void Reset()
        {
            Time = 0;
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(Time);
            bw.Write(unitId);
        }
    }
}
