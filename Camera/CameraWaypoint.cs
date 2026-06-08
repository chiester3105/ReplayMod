using System;
using System.Collections.Generic;
using ReplayMod.Data;
using UnityEngine;

namespace ReplayMod.Camera
{
    public class CameraWaypoint : MonoBehaviour
    {
        public Action onPositionChanged;
        public Action onDestroy;
        GameObject sphere;
        public Action onDelete;
        public bool controlPoint = false;

        public CameraWaypoint Parent { get; private set; }

        private List<CameraWaypoint> _controls = new();
        public static CameraWaypoint Create(Vector3 position, Quaternion rotation = default, double time = 0)
        {
          
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = position;
            sphere.transform.rotation = rotation;
            sphere.transform.localScale = Vector3.one * 2f;
            sphere.transform.SetParent(Datum.origin); 

           
            Collider col = sphere.GetComponent<Collider>();
            col.isTrigger = true;

            CameraWaypoint waypoint = sphere.AddComponent<CameraWaypoint>();
            waypoint.Time = time;
            waypoint.sphere = sphere;
            return waypoint;
        }
        public static CameraWaypoint Create(PositionSnapshot snapshot)
        {
            return Create(snapshot.position.ToLocalPosition(), snapshot.rotation, snapshot.time);
        }
        public double Time;

        private void OnDestroy()
        {
            onPositionChanged = null;
            onDestroy?.Invoke();
            onDestroy = null;
            Destroy(sphere);
            DetachFromParent();
        }

        public void AddControlPoint(CameraWaypoint point)
        {
            _controls.Add(point);
        }
        public void DeleteControlPoint(CameraWaypoint point)
        {
            if(_controls.Contains(point)) _controls.Remove(point);
        }
        public void DetachFromParent()
        {
            if (Parent != null)
            {
                Parent.DeleteControlPoint(this);
            }
        }
        public void SetParent(CameraWaypoint joint)
        {
            Parent = joint;
            controlPoint = true;

            joint.AddControlPoint(this);
        }

        public void MirrorControlPoint(CameraWaypoint mirrorFrom)
        {
            if (mirrorFrom == null || !mirrorFrom.controlPoint)
            {
                Plugin.logger.LogWarning("point is null or not a control point");
                return;
            }

            foreach (var control in _controls)
            {
                if(mirrorFrom != control)
                {
                    Vector3 vector = mirrorFrom.transform.position - mirrorFrom.Parent.transform.position;
                    control.transform.position = control.Parent.transform.position - vector;
                    control.onPositionChanged?.Invoke();
                }
            }
        }
    }
}