using System;
using System.IO;
using ReplayMod.Events.ConcreteEvents;

namespace ReplayMod.Events
{
    public static class ReplayEventFactory
    {
        public static T GetEvent<T>() where T : class, IReplayEvent, new()
            => ReplayEventPool<T>.Get();

        public static void Return<T>(T ev) where T : class, IReplayEvent, new()
            => ReplayEventPool<T>.Return(ev);


        public static IReplayEvent Create(EventType type)
        {
            return type switch
            {
                EventType.Spawn => GetEvent<SpawnEvent>(),
                EventType.Despawn => GetEvent<DespawnEvent>(),
                EventType.Move => GetEvent<UpdatePositionEvent>(),
                EventType.Command => GetEvent<MethodInvokeEvent>(),
                EventType.ControlInputs => GetEvent<UpdateInputsEvent>(),
                EventType.PartDetach => GetEvent<DetachPartEvent>(),
                EventType.ToggleGear => GetEvent<SetGearEvent>(),
                EventType.UpdateTurret => GetEvent<UpdateTurretTransform>(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static IReplayEvent CreateAndRead(EventType type, BinaryReader reader)
        {
            var ev = Create(type);
            ev.Read(reader);
            return ev;
        }

        public static void Return(IReplayEvent ev)
        {
            switch (ev)
            {
                case SpawnEvent e: ReplayEventPool<SpawnEvent>.Return(e); break;
                case UpdatePositionEvent e: ReplayEventPool<UpdatePositionEvent>.Return(e); break;
                case DespawnEvent e: ReplayEventPool<DespawnEvent>.Return(e); break;
                case UpdateInputsEvent e: ReplayEventPool<UpdateInputsEvent>.Return(e); break;
                case DetachPartEvent e: ReplayEventPool<DetachPartEvent>.Return(e); break;
                case SetGearEvent e: ReplayEventPool<SetGearEvent>.Return(e); break;
                case UpdateTurretTransform e: ReplayEventPool<UpdateTurretTransform>.Return(e); break;
                default: throw new ArgumentOutOfRangeException(nameof(ev), ev.GetType().Name);
            }
        }
    }
}