using System;
using System.Collections.Generic;
using System.Linq;
using ReplayMod.Data;
using ReplayMod.Patches;
using Steamworks;
using UnityEngine;

namespace ReplayMod
{
    public class VisualWaypoints : MonoBehaviour
    {
        private void Awake()
        {
            DatumPatch.onShiftOrigin += UpdateLineRender;
            //Even with transform setparent for LineRenderer it teleports after
            //datum origin shift. Calling update line fixes that.
        }
        public List<PositionSnapshot> CameraWaypoints = new();
        private Dictionary<PositionSnapshot, GameObject> _3Dwaypoints = new();
        private LineRenderer _currentLine;
        public bool RenderEnabled { get; private set; } = true;

        public void Reset()
        {
            if (_currentLine != null) Destroy(_currentLine.gameObject);
            foreach (var value in _3Dwaypoints.Values)
            {
                if (value != null) Destroy(value);
            }
            CameraWaypoints.Clear();
            _3Dwaypoints.Clear();
        }

        public void Restore()
        {
            foreach (var key in CameraWaypoints)
            {
                _3Dwaypoints[key] = CreateWaypoint3DMarker(key.position.ToLocalPosition(), key.rotation);
            }
            UpdateLineRender();
        }

        public void AddWaypoint(PositionSnapshot position)
        {
            var marker = CreateWaypoint3DMarker(position.position.ToLocalPosition(), position.rotation);

            CameraWaypoints.Add(position);
            _3Dwaypoints.Add(position, marker);
            UpdateLineRender();
        }

        private GameObject CreateWaypoint3DMarker(Vector3 position, Quaternion quaternion)
        {
            //primitive 3d object
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = position;
            marker.transform.rotation = quaternion;
            marker.transform.localScale = Vector3.one * 2f;

            //color
            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.material = new Material(renderer.sharedMaterial);
            renderer.material.color = Color.yellow;

            //arrow (camera lookforward visual)
            /* GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
             arrow.transform.SetParent(marker.transform);
             arrow.transform.localPosition = Vector3.forward * 2f;
             arrow.transform.localScale = new Vector3(0.5f, 0.5f, 1.5f);
             arrow.GetComponent<Renderer>().material.color = Color.red;
             marker.transform.rotation = quaternion;
            */

            marker.transform.SetParent(Datum.origin);
            var colliders = marker.GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;    
            }
            marker.SetActive(RenderEnabled);
            return marker;
        }

        public void DeleteLast()
        {
            if( CameraWaypoints.Count > 0 )
            {
                var last = CameraWaypoints[CameraWaypoints.Count - 1];
                CameraWaypoints.RemoveAt(CameraWaypoints.Count - 1);

                if(_3Dwaypoints.TryGetValue(last, out var gameObject))
                {
                    Destroy(gameObject);
                    _3Dwaypoints.Remove(last);
                }
            }
            UpdateLineRender();
        }

        private LineRenderer CreateLine()
        {
            GameObject line = new GameObject();
            LineRenderer lr = line.AddComponent<LineRenderer>();
            lr.startWidth = 0.2f;
            lr.endWidth = 0.2f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.yellow;
            lr.endColor = Color.yellow;
            lr.positionCount = 0;
            
            return lr;
        }

        private void UpdateLineRender()
        {
            if (_currentLine != null) Destroy(_currentLine);
            if (CameraWaypoints.Count < 0) return;

            _currentLine = CreateLine();
            _currentLine.positionCount = CameraWaypoints.Count;
            var positions = CameraWaypoints.Select(w => w.position.ToLocalPosition()).ToArray();
            _currentLine.SetPositions(positions);
            _currentLine.transform.SetParent(Datum.origin);
            _currentLine.gameObject.SetActive(RenderEnabled);
        }
        
        
        public void SetRenderActive(bool flag)
        {
            RenderEnabled = flag;
            foreach (var kvp in _3Dwaypoints)
            {
                kvp.Value.SetActive(flag);
            }
            if (_currentLine != null)
                _currentLine.gameObject.SetActive(flag);
        }
    }
}
