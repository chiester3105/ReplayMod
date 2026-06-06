using System;
using System.Collections.Generic;
using System.IO;
using ReplayMod.Core;
using ReplayMod.Data;
using UnityEngine;

namespace ReplayMod
{
    public class UI : MonoBehaviour
    {
        private Texture2D _statusCircleTexture;
        private Texture2D _timelinePointCircleTexture;
        private float _blinkSpeed = 5f;
        private bool _enabled = false;
        private void CreateCircleTexture(int size, out Texture2D texture)
        {
            texture = new Texture2D(size, size);
            Color[] colors = new Color[size * size];
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius;
                    float dy = y - radius;
                    if (dx * dx + dy * dy <= radius * radius)
                        colors[y * size + x] = Color.white;
                    else
                        colors[y * size + x] = Color.clear;
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
        }
        private void Awake()
        {

        }
        private void OnEnable()
        {

        }
        private void OnDisable()
        {

        }

        private Rect windowRect = new Rect(40, 40, 310, 500);
        private void OnGUI()
        {
            if (_enabled)
            {
                windowRect = GUI.Window(0, windowRect, Draw, "");
            }
            DrawTimeline();
            DisplayCurrentStatus();
            DrawTimelineMarkers();
        }

        private void Draw(int windowId)
        {
            DrawPanel();
            DrawScrollMenu();
            DrawButtons();
            GUI.DragWindow();
        }

        private void DrawPanel()
        {
            GUI.color = Color.yellow;
            GUI.Box(new Rect(0, 0, 310, 500), "");
        }
        private void DrawButtons()
        {
            if (ReplayManager.i.GetState() == ModStates.Replay)
            {
                string text = _showTimeline ? "Hide timeline" : "Show timeline";
                if (GUI.Button(new Rect(5, 460, 95, 25), text))
                {
                    ToggleTimeline();
                }
                if (GUI.Button(new Rect(5, 455, 95, 25), "Cam flight"))
                {
                    ReplayManager.i.StartCamFlight();
                }
                string text2 = TimeScaleManager.Scale == 0 ? "Playback" : "Pause";
                if (GUI.Button(new Rect(110, 460, 95, 25), text2))
                {
                    ReplayManager.i.TogglePause();
                }
                if (GUI.Button(new Rect(215, 460, 95, 25), "Quit"))
                {
                    ReplayManager.i.SwitchState(ModStates.Idle);
                }
            }
            else if (ReplayManager.i.GetState() == ModStates.Record)
            {
                if (GUI.Button(new Rect(15, 460, 100, 40), "Stop record"))
                {
                    ReplayManager.i.SwitchState(ModStates.Idle);
                }
            }
            else if (ReplayManager.i.GetState() == ModStates.Idle)
            {
                if (GUI.Button(new Rect(15, 460, 100, 40), "Start record"))
                {
                    ReplayManager.i.SwitchState(ModStates.Record);
                }
            }
            
            
        }
        Vector2 scrollPosition;
        private void DrawScrollMenu()
        {
            var replayFiles = GetFilesName();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400), GUILayout.Width(300));
            foreach (string file in replayFiles)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(file);
                if (GUILayout.Button("Play", GUILayout.Width(50)))
                {
                    Plugin.logger.LogInfo("Playing: " + file);
                    ReplayManager.i.StartPlaying(file);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }
        
        private void DisplayCurrentStatus()
        {
            if (_statusCircleTexture == null) CreateCircleTexture(20, out _statusCircleTexture);
            if (ReplayManager.i.GetState() != ModStates.Record) return;
            float alpha = 0.3f + 0.7f * (Mathf.Sin(Time.time * _blinkSpeed) * 0.5f + 0.5f);

            GUI.color = new Color(1f, 0f, 0f, alpha);
            Rect rect = new Rect(10, 10, 20, 20);
            GUI.DrawTexture(rect, _statusCircleTexture);

            GUI.color = Color.white; 
        }

        public void Toggle()
        {
            _enabled = !_enabled;
        }
        private IEnumerable<string> GetFilesName()
        {
            var paths = ReplayManager.i.GetFiles();
            foreach (var path in paths)
            {
                yield return Path.GetFileName(path);
            }
        }
        private bool _draggingSlider;
        private double _dragTime;
        private bool _showTimeline = true;
        private void ToggleTimeline()
        {
            _showTimeline = !_showTimeline;
        }
        private void DrawTimeline()
        {
            if (ReplayManager.i.GetState() == ModStates.Replay && _showTimeline) {

                double duration = ReplayManager.i.GetDuration();
                double currentTime = ReplayManager.i.GetCurrentVirtualTime();

                float sliderValue = _draggingSlider ? (float)_dragTime : (float)currentTime;

                float newValue = GUI.HorizontalSlider(
                    new Rect(0, 0, Screen.width, 30),
                    sliderValue,
                    0f,
                    (float)duration
                );

                if (_draggingSlider)
                {
                    _dragTime = newValue;

                }
                else if (Math.Abs(newValue - currentTime) > 0.01f)
                {
                    _draggingSlider = true;
                    _dragTime = newValue;
                }

                if (_draggingSlider && Event.current.type == EventType.MouseUp)
                {
                    ReplayManager.i.TimelineJump(_dragTime);
                    _draggingSlider = false;
                }

                GUI.Label(new Rect(Screen.width / 2 - 10, 10, 1000, 20), $"{currentTime:F1}s / {duration:F1}s");
            } 
        }

        private List<PositionSnapshot> _waypoints = new();
        public void DeleteLastWaypoint()
        {
            if (_waypoints.Count > 0)
            {
                _waypoints.RemoveAt(_waypoints.Count - 1);
            }
        }
        public void AddWaypoint(PositionSnapshot snapshot)
        {
            if (_timelinePointCircleTexture == null) CreateCircleTexture(7, out _timelinePointCircleTexture);

            _waypoints.Add(snapshot);
        }

        private void DrawTimelineMarkers()
        {
            foreach (var waypoint in _waypoints)
            {
                var t =  waypoint.time / ReplayManager.i.GetDuration() ;
                float x = (float)t * Screen.width;
                
                GUI.color = Color.yellow;
                Rect rect = new Rect(x, 2, 10, 10);
                GUI.DrawTexture(rect, _timelinePointCircleTexture);

                GUI.color = Color.white;
            }
        }
    }
}
