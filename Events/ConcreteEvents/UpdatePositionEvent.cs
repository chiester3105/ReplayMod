using UnityEngine;
using System.IO;
using ReplayMod.Data;
using ReplayMod.Misc;
using ReplayMod.Core;


namespace ReplayMod.Events.ConcreteEvents
{
    [ReplayEvent(EventType.Move)]
    public class UpdatePositionEvent : IReplayEvent
    {
        public EventType EventType { get; } = EventType.Move;
        public double Time { get; set; }

        public uint unitId;
        public GlobalPosition position;
        public Quaternion rotation;
        public Vector3 velocity;

        public const int QUANT = 100;
        public const int ROT_QUANT = 1000;
        public bool isAbs;
        public void Execute(object worker)
        {
            if(worker is UnitController controller)
                controller.Execute(this);
        }

        public void Read(BinaryReader br)
        {
            unitId = (uint)br.ReadInt32();
            Time = br.ReadDouble();

            //post 2.0.0
            //isAbs = br.ReadBoolean();
            //EventsHelper.ReadCompressedPosition(this, br);

            //Plugin.logger.LogInfo($"UPE: {position}, r{rotation}, v{velocity}");
            // pre 2.0.0 

            position = new GlobalPosition()
            {
                x = br.ReadSingle(),
                y = br.ReadSingle(),
                z = br.ReadSingle()
            };

            rotation = new Quaternion()
            {
                x = br.ReadSingle(),
                y = br.ReadSingle(),
                z = br.ReadSingle(),
                w = br.ReadSingle()
            };

            velocity = new Vector3()
            {
                x = br.ReadSingle(),
                y = br.ReadSingle(),
                z = br.ReadSingle(),
            };
        }

        public void Reset()
        {
            unitId = default;
            position = default;
            rotation = default;
            velocity = default;
            //ReplayEventFactory.Return(this);
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(unitId);
            bw.Write(Time);

            //post 2.0.0
            //EventsHelper.WriteCompressedPosition(this, bw);
            //Plugin.logger.LogInfo($"UPE: {position}, r{rotation}, v{velocity}");
            //pre 2.0.0 

            bw.Write(position.x);
            bw.Write(position.y);
            bw.Write(position.z);

            bw.Write(rotation.x);
            bw.Write(rotation.y);
            bw.Write(rotation.z);
            bw.Write(rotation.w);

            bw.Write(velocity.x);
            bw.Write(velocity.y);
            bw.Write(velocity.z);

            //Plugin.logger.LogInfo($"Update position written to file: {unitId} {position}");
        }

        public void CopyFromSnapshot(PositionSnapshot snapshot)
        {
            position = snapshot.position;
            rotation = snapshot.rotation;
            velocity = snapshot.velocity;
            Time = snapshot.time;
        }
    }
}
