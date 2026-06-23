using System.IO;
using ReplayMod.Core;

namespace ReplayMod.Events.ConcreteEvents
{
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


        public void Execute(object worker = null)
        {
            if(worker is UnitController controller)
            {
                //controller.ApplyInputs(this);
            }
        }

        public void Read(BinaryReader br)
        {
            Time = br.ReadDouble();
            id = br.ReadUInt32();
            pitch = br.ReadSingle();
            roll = br.ReadSingle();
            yaw = br.ReadSingle();
            throttle = br.ReadSingle();
            brake = br.ReadSingle();
            customAxis1 = br.ReadSingle();
        }

        public void Reset()
        {
            Time = 0;

        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(Time);
            bw.Write(id);
            bw.Write(pitch);
            bw.Write(roll);
            bw.Write(yaw);
            bw.Write(throttle);
            bw.Write(brake);
            bw.Write(customAxis1);
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
