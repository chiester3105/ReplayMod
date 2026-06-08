using System.Collections.Generic;
using ReplayMod.Patches;
using UnityEngine;

namespace ReplayMod.Camera
{
    public class SplineSegment
    {
        //joints
        private CameraWaypoint p0;
        private CameraWaypoint p3;

        //control points
        private CameraWaypoint p1;
        private CameraWaypoint p2;

        private LineRenderer curveVisualizer;
        private LineRenderer p0p1_line;
        private LineRenderer p2p3_line;
        private Color segmentColor;
        private int curvePointsCount = 40;

        public SplineSegment(CameraWaypoint p0, CameraWaypoint p3)
        {
            //default position for control points - line between joints
            Vector3 p1_position = Vector3.Lerp(p0.transform.position, p3.transform.position, 0.3f);
            Vector3 p2_position = Vector3.Lerp(p0.transform.position, p3.transform.position, 0.6f);

            segmentColor = CameraManager.i.GetSegmentColor();

            this.p0 = p0;
            this.p3 = p3;
            //auto generating control points ont p0p3 line, 1/3 and 2/3
            p1 = CameraWaypoint.Create(p1_position, default);
            p2 = CameraWaypoint.Create(p2_position, default);

            p1.SetParent(p0);
            p2.SetParent(p3);

            p0p1_line = CreateLine();
            p2p3_line = CreateLine();
            curveVisualizer = CreateLine();

            //all triggers to recalculate line
            Subscribe();
            DatumPatch.onShiftOrigin += UpdateLines;

            UpdateLines();
            SaveSegment();
        }

        private Vector3[] p0p1Buffer = new Vector3[2];
        private Vector3[] p2p3Buffer = new Vector3[2];
        private Vector3[] curveBuffer;
        private void UpdateLines()
        {
            if (p0p1_line == null) return;
            if (p2p3_line == null) return;
            if (curveVisualizer == null) return;

            
            p0p1Buffer[0] = p0.transform.position;
            p0p1Buffer[1] = p1.transform.position;
            p0p1_line.positionCount = 2;
            p0p1_line.SetPositions(p0p1Buffer);

            p2p3Buffer[0] = p2.transform.position;
            p2p3Buffer[1] = p3.transform.position;
            p2p3_line.positionCount = 2;
            p2p3_line.SetPositions(p2p3Buffer);

            
            if (curveBuffer == null || curveBuffer.Length != curvePointsCount)
                curveBuffer = new Vector3[curvePointsCount];

            curveVisualizer.positionCount = curvePointsCount;
            float step = 1.0f / curvePointsCount;

            for (int i = 0; i < curvePointsCount; i++)
            {
                float t = step * i;
                curveBuffer[i] = Interpolate(p0.transform.position,
                                             p1.transform.position,
                                             p2.transform.position,
                                             p3.transform.position,
                                             t);
            }
            curveBuffer[0] = p0.transform.position;
            curveBuffer[curvePointsCount - 1] = p3.transform.position;
            curveVisualizer.SetPositions(curveBuffer);
        }

        private LineRenderer CreateLine()
        {
            GameObject line = new GameObject();
            LineRenderer lr = line.AddComponent<LineRenderer>();

            lr.startWidth = 0.2f;
            lr.endWidth = 0.2f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = segmentColor;
            lr.endColor = segmentColor;
            lr.positionCount = 0;

            return lr;
        }

        private Vector3 Interpolate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t); //just to be sure
            
            //Bezier cubic curve formula
            var v1 = (1 - t) * (1 - t) * (1 - t) * p0;
            var v2 = 3 * (1 - t) * (1 - t) * t * p1;
            var v3 = 3 * (1 - t) * t * t * p2;
            var v4 = t * t * t * p3;

            return v1 + v2 + v3 + v4;
        }

        public bool TryInterpolate(double time, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if(p0.Time <= time && time <= p3.Time)
            {
                float t = (float)((time - p0.Time) / (p3.Time - p0.Time));
                position = Interpolate(p0.transform.position,
                               p1.transform.position,
                               p2.transform.position,
                               p3.transform.position,
                               t);
                rotation = Quaternion.Slerp(p0.transform.rotation, p3.transform.rotation, t);
                return true;
            }
            return false;
        }

        private void Subscribe()
        {
            p0.onPositionChanged += UpdateLines;
            p1.onPositionChanged += UpdateLines;
            p2.onPositionChanged += UpdateLines;
            p3.onPositionChanged += UpdateLines;

            p0.onPositionChanged += SaveSegment;
            p1.onPositionChanged += SaveSegment;
            p2.onPositionChanged += SaveSegment;
            p3.onPositionChanged += SaveSegment;

            p3.onDelete += Delete;
        }

        private void SaveSegment()
        {
            var savedData = SplineSegmentSave.Create(p0, p1, p2,p3);
            CameraManager.i.SaveData(this, savedData);
        }

        public void Restore(SplineSegmentSave save)
        {
            p0 = CameraManager.i.GetConnectionPoint();
            p1 = CameraWaypoint.Create(save.p1);
            p2 = CameraWaypoint.Create(save.p2);
            p3 = CameraWaypoint.Create(save.p3);

            p1.SetParent(p0);
            p2.SetParent(p3);

            CameraManager.i.AddSegmentEnd(p3);

            Subscribe();

            p0p1_line = CreateLine();
            p2p3_line = CreateLine();
            curveVisualizer = CreateLine();
            UpdateLines();
        }

        public void Delete()
        {
            p0.onPositionChanged -= UpdateLines;
            p0.onPositionChanged -= SaveSegment;
            DatumPatch.onShiftOrigin -= UpdateLines;
            UnityEngine.Object.Destroy(p2);
            UnityEngine.Object.Destroy(p1);
            UnityEngine.Object.Destroy(p0p1_line);
            UnityEngine.Object.Destroy(p2p3_line);
            UnityEngine.Object.Destroy(curveVisualizer);
            CameraManager.i.RemoveSpline(this);
        }
    }
}
