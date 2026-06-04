using System.IO;
using ReplayMod.Core;

namespace ReplayMod.Events.ConcreteEvents
{
    public class DetachPartEvent : IReplayEvent
    {
        public double Time { get; set; }

        public EventType EventType { get; } = EventType.PartDetach;

        public uint unitID;
        public byte partID;
        public void Execute(object worker = null)
        {
            if (worker is UnitController u)
            {
                u.DetachPart(this);
            }
        }

        public void Read(BinaryReader br)
        {
            Plugin.SafeLog("Reading detach part packet");
            Time = br.ReadDouble();
            unitID = br.ReadUInt32();
            partID = br.ReadByte();
        }

        public void Reset()
        {
            
        }

        public void Write(BinaryWriter bw)
        {
            Plugin.SafeLog("writing detach part packet");
            bw.Write(Time);
            bw.Write(unitID);
            bw.Write(partID);
        }
    }
}
