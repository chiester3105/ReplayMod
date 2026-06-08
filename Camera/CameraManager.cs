using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using ReplayMod.Core;
using ReplayMod.Data;
using RuntimeHandle;
using UnityEngine;

namespace ReplayMod.Camera
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager i {  get; private set; }
        private WaypointEditMenu _waypointEdit;

        private void Start()
        {
            if (i != null)
            {
                Destroy(gameObject);
                return;
            }

            i = this;
            DontDestroyOnLoad(gameObject);

            _waypointEdit = gameObject.AddComponent<WaypointEditMenu>();
            DontDestroyOnLoad(_waypointEdit);

            Subscribe().Forget();
        }

        private async UniTask Subscribe()
        {
            await UniTask.WaitUntil(() => ReplayManager.i != null);
            ReplayManager.i.onReset += Restore;
        }

        private void Update()
        {
            HandleInputs();
            if (_isDragging) _pointOnEdit.onPositionChanged.Invoke();
            MoveCamera(ReplayManager.i.GetCurrentVirtualTime());
        }

        private void HandleInputs()
        {
            if (Input.GetMouseButtonDown(0) && CameraStateManager.i.mainCamera != null)
            {
                Plugin.logger.LogInfo("Start dragging waypoint");
                Ray ray = CameraStateManager.i.mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if(Physics.Raycast(ray, out hit))
                {
                    Plugin.logger.LogInfo(hit);
                    if (hit.transform.TryGetComponent<CameraWaypoint>(out var component))
                    {
                        Plugin.logger.LogInfo("Exctracted waypoint from raycast hit, attaching handle");
                        AttachHandleToWaypoint(component);
                    }
                }
            }
            if (ConfigManager.CreateCameraWaypoint.Value.GetDown() && CameraStateManager.i.mainCamera != null)
            {
                AddPoint();
            }
            if (ConfigManager.DeleteLastCameraWaypoint.Value.GetDown())
            {
                DeleteLastPoint();
            }

            if(ConfigManager.MirrorControlPoint.Value.GetDown())
            {
                if (_pointOnEdit == null) return;

                _pointOnEdit.Parent.MirrorControlPoint(_pointOnEdit);
            }

            if (ConfigManager.DeselectWaypoint.Value.GetDown())
                Deselect();

            if(Input.GetKeyDown(KeyCode.K))
            {
                //_waypointEdit.Toggle();
                //ill add waypoint time edit in next version
            }
        }


        private RuntimeTransformHandle _activeHandle;
        private CameraWaypoint _pointOnEdit;
        private void AttachHandleToWaypoint(CameraWaypoint waypoint)
        {
            if (_activeHandle != null)
                Destroy(_activeHandle.gameObject);

            _activeHandle = RuntimeTransformHandle.Create(waypoint.transform, HandleType.POSITION);

            _activeHandle.space = HandleSpace.WORLD;   
            _activeHandle.axes = HandleAxes.XYZ;       
            _activeHandle.autoScale = true;            
            _activeHandle.autoScaleFactor = 1f;

            _activeHandle.startedDraggingHandle.AddListener(OnDragStart);
            _activeHandle.endedDraggingHandle.AddListener(OnDragEnd);

            _pointOnEdit = waypoint;
        }
        private void Deselect()
        {
            if(_activeHandle !=null)
            {
                Destroy(_activeHandle.gameObject);
                _activeHandle = null;
            }
        }

        private bool _isDragging = false;
        private void OnDragStart()
        {
            _isDragging = true;
        }

        private void OnDragEnd()
        {
            _isDragging= false;
        }
        private Dictionary<SplineSegment, SplineSegmentSave> _segmentsData = new();
        private List<PositionSnapshot> _positions = new();
        private List<CameraWaypoint> _waypoints = new();
        private CameraWaypoint _start;
        public CameraWaypoint GetConnectionPoint()
        {
            if (_start == null)
            {
                var waypoint = CameraWaypoint.Create(_positions[0]);
                _start = waypoint;
                _waypoints.Add(waypoint);
                return waypoint;
            }
            else
            {
                return _waypoints[_waypoints.Count - 1];
            }

        }
        public void AddSegmentEnd(CameraWaypoint point)
        {
            _waypoints.Add(point);
        }
        private void Restore()
        {
            _waypoints.Clear();
            _start = null;
            foreach (var kvp in _segmentsData)
            {
                kvp.Key.Restore(kvp.Value);
            }
        }
        public void SaveData(SplineSegment segment, SplineSegmentSave data)
        {
            _segmentsData[segment] = data;
        }
        private void AddPoint()
        {
            var time = ReplayManager.i.GetCurrentVirtualTime();
            if (_waypoints.Count > 0 && time <= _waypoints[_waypoints.Count - 1].Time )
            {
                return;
            }
            CameraStateManager.i.GetCameraPosition(out var posRot);
            var snapshot = new PositionSnapshot()
            {
                position = posRot.Position,
                rotation = posRot.Rotation,
                time = time,
            };
            _positions.Add(snapshot);
            var waypoint = CameraWaypoint.Create(snapshot.position.ToLocalPosition(), snapshot.rotation, snapshot.time);
            if (_start == null)
            {
                _start = waypoint;
            }
            else
            {
                var segment = new SplineSegment(_waypoints[_waypoints.Count - 1], waypoint);
            }
            _waypoints.Add(waypoint);
        }

        private void DeleteLastPoint()
        {
            if (_waypoints.Count == 0) return;

            var point = _waypoints[_waypoints.Count - 1];
            point.onDelete?.Invoke();
            Destroy(point);
            _waypoints.Remove(point);
            _positions.RemoveAt(_positions.Count - 1);
        }
        public void RemoveSpline(SplineSegment segment)
        {
            if(_segmentsData.ContainsKey(segment))
            {
                _segmentsData.Remove(segment);
            }
        }
        Color[] colors = { Color.red, Color.green, Color.blue };
        public Color GetSegmentColor()
        {
            return colors[_segmentsData.Count %  colors.Length];
        }

        private bool _isMoving = false;
        public void SetMoveCamera(bool moveCamera)
        {
            _isMoving = moveCamera;
        }
        private void MoveCamera(double time)
        {
            if (!_isMoving) return;
            foreach(var segment in _segmentsData.Keys)
            {
                if(segment.TryInterpolate(time, out var position, out var rotation))
                {
                    PositionRotation posRot = new()
                    {
                        Position = position.ToGlobalPosition(),
                        Rotation = rotation,
                    };
                    CameraStateManager.i.SetCameraPosition(posRot);
                }
            }
        }

        public double GetStartingTime()
        {
            if (_positions.Count > 0)
                return _positions[0].time;
            else return 0;
        }

        public void StartCamFlight()
        {
            ReplayManager.i.TimelineJump(GetStartingTime());
            SetMoveCamera(true);
        }
        public void StopCamFlight()
        {
            SetMoveCamera(false);
        }
        public List<CameraWaypoint> GetWaypoints()
        {
            return _waypoints;
        }

        public bool IsEditing()
        {
            return _pointOnEdit != null;
        }
        public double GetWaypointTime()
        {
            if (_pointOnEdit != null)
                return _pointOnEdit.Time;
            else return 0;
        }
        public void SetNewTime(double time)
        {
            if(_pointOnEdit != null)
            {
                int idx = _waypoints.IndexOf(_pointOnEdit);
                double newTime;
                if (0 < idx && idx < _waypoints.Count - 1)
                {
                    newTime = Math.Clamp(time, _waypoints[idx - 1].Time, _waypoints[idx + 1].Time);
                    
                }
                else if (idx == 0)
                {
                    double max;
                    if (_waypoints.Count > 1)
                        max = _waypoints[idx + 1].Time;
                    else
                        max = ReplayManager.i.GetDuration();
                    newTime = Math.Clamp(time, 0, max);
                }
                else
                {
                    double min = _waypoints[idx - 1].Time;
                    double max = ReplayManager.i.GetDuration();
                    newTime = Math.Clamp(time, min, max);
                }
                _pointOnEdit.Time = newTime;
            }
        }
        public bool ControlPointSelected()
        {
            return _pointOnEdit != null && _pointOnEdit.controlPoint;
        }

    }
}
