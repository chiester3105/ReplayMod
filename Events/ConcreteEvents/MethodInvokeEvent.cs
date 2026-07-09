using System;
using System.IO;

namespace ReplayMod.Events.ConcreteEvents
{
    [ReplayEvent(EventType.Command)]
    public class MethodInvokeEvent : IReplayEvent
    {
        public uint unitId;
        public uint methodId;
        public int argsCount;
        public object[] args;
        public EventType EventType { get; } = EventType.Command;
        public double Time { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Execute(object worker)
        {
            throw new NotImplementedException();
        }

        public void Read(BinaryReader br)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
           // ReplayEventFactory.Return(this);
        }

        public void Write(BinaryWriter bw)
        {
            throw new NotImplementedException();
        }
    }
}
