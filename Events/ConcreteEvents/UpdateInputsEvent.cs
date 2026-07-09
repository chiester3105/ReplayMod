using System.IO;
using ReplayMod.Core;
using ReplayMod.Misc;

namespace ReplayMod.Events.ConcreteEvents
{
    [ReplayEvent(EventType.ControlInputs)]
    public class UpdateInputsEvent : IReplayEvent
    {
        public double Time { get; set; }

        public EventType EventType { get; } = EventType.ControlInputs;
        public uint id;

        
        public float pitch;
        public float roll;
        public float yaw;
        public float throttle;
        public float brake;
        public float customAxis1;

        public byte mask;
        public const int QUANT = 10000;
        public void Execute(object worker = null)
        {
            if(worker is UnitController controller)
            {
                controller.Execute(this);
            }
        }

        public void Read(BinaryReader br)
        {
            Time = br.ReadDouble();
            id = br.ReadUInt32();
            mask = br.ReadByte();

            var inputs = EventsHelper.GetInputs(id, mask, br, QUANT);

            pitch = inputs.pitch;
            roll = inputs.roll;
            yaw = inputs.yaw;
            throttle = inputs.throttle;
            brake = inputs.brake;
            customAxis1 = inputs.customAxis1;
        }

        public void Reset()
        {
            Time = 0;

        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(Time);
            bw.Write(id);

            EventsHelper.GetInputsMask(this, out mask);
            bw.Write(mask);

            if ((mask & 0b0000_0001) != 0)
                bw.Write((ushort)(pitch * QUANT));
            if ((mask & 0b0000_0010) != 0)
                bw.Write((ushort)(roll * QUANT));
            if ((mask & 0b0000_0100) != 0)
                bw.Write((ushort)(yaw * QUANT));
            if ((mask & 0b0000_1000) != 0)
                bw.Write((ushort)(throttle * QUANT));
            if ((mask & 0b0001_0000) != 0)
                bw.Write((ushort)(brake * QUANT));
            if ((mask & 0b0010_0000) != 0)
                bw.Write((ushort)(customAxis1 * QUANT));
        }

        public void CopyFromControlInputs(ControlInputs source)
        {
            pitch = source.pitch;
            roll = source.roll;
            yaw = source.yaw;
            throttle = source.throttle;
            brake = source.brake;
            customAxis1 = source.customAxis1;
        }

        public void CopyToControlInputs(ControlInputs inputs)
        {
            inputs.pitch = pitch;
            inputs.roll = roll;
            inputs.yaw = yaw;
            inputs.throttle = throttle;
            inputs.brake = brake;
            inputs.customAxis1 = customAxis1;
        }

        
    }
}
