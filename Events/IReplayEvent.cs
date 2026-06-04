using System.IO;

namespace ReplayMod.Events
{
    public interface IReplayEvent 
    {
        public double Time { get; set; }
        public EventType EventType { get; }
        public void Reset();
        public void Write(BinaryWriter bw);
        public void Read(BinaryReader br);
        public void Execute(object worker = null); 
    }
}
