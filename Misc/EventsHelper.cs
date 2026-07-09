using System;
using System.Collections.Generic;
using System.IO;
using Mirage.Serialization;
using ReplayMod.Data;
using ReplayMod.Events.ConcreteEvents;
using UnityEngine;

namespace ReplayMod.Misc
{
    public static class EventsHelper
    {
        public static void Reset()
        {
            _lastInputs.Clear();
            _actualPositions.Clear();
            _steps.Clear();
        }

        private static Dictionary<uint, Inputs> _lastInputs = new();
        public static void GetInputsMask(UpdateInputsEvent e, out byte mask)
        {
            mask = 0;
            if (!_lastInputs.TryGetValue(e.id, out var inputs))
            {
                _lastInputs[e.id] = new Inputs()
                {
                    pitch = e.pitch,
                    roll = e.roll,
                    yaw = e.yaw,
                    throttle = e.throttle,
                    brake = e.brake,
                    customAxis1 = e.customAxis1
                };
                mask = 0b0011_1111;
                return;
            }
            
            if (e.pitch.ValueChanged(inputs.pitch))
            {
                mask |= (1 << 0);
                inputs.pitch = e.pitch;
            }
            if (e.roll.ValueChanged(inputs.roll))
            {
                mask |= (1 << 1);
                inputs.roll = e.roll;
            }
            if(e.yaw.ValueChanged(inputs.yaw))
            {
                mask |= (1 << 2);
                inputs.yaw = e.yaw;
            }
            if(e.throttle.ValueChanged(inputs.throttle))
            {
                mask |= (1 << 3);
                inputs.throttle = e.throttle;
            }
            if (e.brake.ValueChanged(inputs.brake))
            {
                mask |= (1 << 4);
                inputs.brake = e.brake;
            }
            if (e.customAxis1.ValueChanged(inputs.customAxis1))
            {
                mask |= (1 << 5);
                inputs.customAxis1 = e.customAxis1;
            }
        }
        public static Inputs GetInputs(uint id, byte mask, BinaryReader br, int quant)
        {
            Inputs inputs;
            if (!_lastInputs.TryGetValue(id, out inputs)) 
            { 
                inputs = new Inputs();
                _lastInputs[id] = inputs;
            }

            if ((mask & 0b0000_0001) != 0)
                inputs.pitch = ((float)br.ReadInt16()) / quant;
            
            if ((mask & 0b0000_0010) != 0)
                inputs.roll = ((float)br.ReadInt16()) / quant;

            if ((mask & 0b0000_0100) != 0)
                inputs.yaw = ((float)br.ReadInt16()) / quant;

            if ((mask & 0b0000_1000) != 0)
                inputs.throttle = ((float)br.ReadInt16()) / quant;

            if ((mask & 0b0001_0000) != 0)
                inputs.brake = ((float)br.ReadInt16()) / quant;

            if ((mask & 0b0010_0000) != 0)
                inputs.customAxis1 = ((float)br.ReadInt16()) / quant;

           return inputs;
        }
        public static bool ValueChanged(this float a, float b)
        {
            if (Math.Abs(a - b) > 0.0001f)
                return true;
            return false;
        }

        private const byte ABS_INTERVAL = 10;
        private static Dictionary<uint, PositionSnapshot> _actualPositions = new();
        private static Dictionary<uint, byte> _steps = new();
        public static void WriteCompressedPosition(UpdatePositionEvent e, BinaryWriter bw)
        {
            if(!_actualPositions.ContainsKey(e.unitId))
            {
                var snapshot = new PositionSnapshot()
                {
                    position = e.position,
                    velocity = e.velocity,
                    rotation = e.rotation,
                };
                _actualPositions[e.unitId] = snapshot;
            }
            if(ShouldWriteAbs(e.unitId))
            {
                //abs flag
                bw.Write(true);

                //vel and pos data
                bw.Write(e.position.x);
                bw.Write(e.position.y);
                bw.Write(e.position.z);

                bw.Write(e.velocity.x);
                bw.Write(e.velocity.y);
                bw.Write(e.velocity.z);
            }
            else
            {
                //abs flag
                bw.Write(false);
                var prev = _actualPositions[e.unitId];
                //vel and pos data
                bw.Write((short)((e.position.x - prev.position.x) * UpdatePositionEvent.QUANT));
                bw.Write((short)((e.position.y - prev.position.y) * UpdatePositionEvent.QUANT));
                bw.Write((short)((e.position.z - prev.position.z) * UpdatePositionEvent.QUANT));

                bw.Write((short)((e.velocity.x - prev.velocity.x) * UpdatePositionEvent.QUANT));
                bw.Write((short)((e.velocity.y - prev.velocity.y) * UpdatePositionEvent.QUANT));
                bw.Write((short)((e.velocity.z - prev.velocity.z) * UpdatePositionEvent.QUANT));
            }
            uint packed = QuaternionPacker.PackAsInt(e.rotation);
            bw.Write(packed);

            var newSnapshot = new PositionSnapshot()
            {
                position = e.position,
                rotation = e.rotation,
                velocity = e.velocity,
            };
            _actualPositions[e.unitId] = newSnapshot;
        }
        public static void ReadCompressedPosition(UpdatePositionEvent e, BinaryReader br)
        {
            if(e.isAbs)
            {             
                e.position = new GlobalPosition()
                {
                    x = br.ReadSingle(),
                    y = br.ReadSingle(),
                    z = br.ReadSingle()
                };

                e.velocity = new Vector3()
                {
                    x = br.ReadSingle(),
                    y = br.ReadSingle(),
                    z = br.ReadSingle(),
                };          
            }
            else
            {
                // first packet should always be abs, so i think i dont have to make a check
                var prevSnapshot = _actualPositions[e.unitId];

                var position = prevSnapshot.position;
                position.x += ((float)br.ReadInt16()) / UpdatePositionEvent.QUANT;
                position.y += ((float)br.ReadInt16()) / UpdatePositionEvent.QUANT;
                position.z += ((float)br.ReadInt16()) / UpdatePositionEvent.QUANT;

                var velocity = prevSnapshot.velocity;
                velocity.x += ((float)br.ReadInt16()) / UpdatePositionEvent.QUANT;
                velocity.y += ((float)br.ReadInt16()) / UpdatePositionEvent.QUANT;
                velocity.z += ((float)br.ReadInt16()) / UpdatePositionEvent.QUANT;


                e.position = position;
                e.velocity = velocity;
            }
            uint packed = br.ReadUInt32();
            e.rotation = QuaternionPacker.UnpackFromInt(packed);

            var newSnapshot = new PositionSnapshot()
            {
                position = e.position,
                rotation = e.rotation,
                velocity = e.velocity,
            };
            _actualPositions[e.unitId] = newSnapshot;

            Plugin.logger.LogWarning($"[{e.isAbs}][{e.unitId}] p{e.position} r{e.rotation} v{e.velocity}");
        }
        private static bool ShouldWriteAbs(uint id)
        {
            if (!_steps.ContainsKey(id))
            {
                _steps[id] = ABS_INTERVAL;
                return true;
            }

            var steps = _steps[id];
            if (steps > 0)
            {
                _steps[id] = (byte)(steps - 1);
                return false;
            }
            else
                _steps[id] = ABS_INTERVAL;
            return true;
        }

        public static void OnSpawn(SpawnEvent e)
        {

        }
        public static void OnDespawn(DespawnEvent e)
        {

        }
    }
}
