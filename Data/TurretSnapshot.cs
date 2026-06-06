using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReplayMod.Events.ConcreteEvents;

namespace ReplayMod.Data
{
    public struct TurretSnapshot
    {
        public float elevationAngle;
        public float traverseAngle;
        public uint ownerId;
        public double time;
        public byte weaponStationIdx;
        public byte turretIdx;
        public static TurretSnapshot Create(UpdateTurretTransform e)
        {
            return new TurretSnapshot
            {
                elevationAngle = e.elevationAngle,
                traverseAngle = e.traverseAngle,
                time = e.Time,
                ownerId = e.attachedUnitId,
                weaponStationIdx = e.weaponStationIdx,
                turretIdx = e.turretIdx,
            };
        }
    }
}
