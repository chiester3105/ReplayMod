using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReplayMod.Data;

namespace ReplayMod.Camera
{
    public struct SplineSegmentSave
    {
        public PositionSnapshot p0;
        public PositionSnapshot p1;
        public PositionSnapshot p2;
        public PositionSnapshot p3;

        public static SplineSegmentSave Create(CameraWaypoint p0,
            CameraWaypoint p1, CameraWaypoint p2, CameraWaypoint p3)
        {
            var p0Save = new PositionSnapshot
            {
                position = p0.transform.position.ToGlobalPosition(),
                rotation = p0.transform.rotation,
                time = p0.Time
            };
            var p1Save = new PositionSnapshot
            {
                position = p1.transform.position.ToGlobalPosition(),
                rotation = p1.transform.rotation,
                time = p1.Time
            };
            var p2Save = new PositionSnapshot
            {
                position = p2.transform.position.ToGlobalPosition(),
                rotation = p2.transform.rotation,
                time = p2.Time
            };
            var p3Save = new PositionSnapshot
            {
                position = p3.transform.position.ToGlobalPosition(),
                rotation = p3.transform.rotation,
                time = p3.Time
            };

            return new SplineSegmentSave
            {
                p0 = p0Save,
                p1 = p1Save,
                p2 = p2Save,
                p3 = p3Save
            };
        }
    }
}
