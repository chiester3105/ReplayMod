using ReplayMod.Events.ConcreteEvents;
using UnityEngine;

namespace ReplayMod.Data
{
    public struct PositionSnapshot
    {
        public GlobalPosition position;
        public Quaternion rotation;
        public Vector3 velocity;
        public double time;

        public static PositionSnapshot Create(UpdatePositionEvent e)
        {
            return new PositionSnapshot
            {
                position = e.position,
                rotation = e.rotation,
                velocity = e.velocity,
                time = e.Time
            };
        }
    }
}
