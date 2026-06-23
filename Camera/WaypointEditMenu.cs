using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ReplayMod.Camera
{
    public class WaypointEditMenu : MonoBehaviour
    {
        private Rect windowRect = new Rect(Screen.width-200, Screen.height-200, 200, 200);

        private bool _enabled = false;

        private void OnGUI()
        {
            if (!_enabled) return;
            windowRect = GUI.Window(0, windowRect, Draw, "");
        }

        private void Draw(int id)
        {
            DrawPanel();
            if(CameraManager.i.IsControlPointSelected())
            {
                DrawTimeEdit();
            }
        }

        private void DrawTimeEdit()
        {
            string text = "Current point time: ";
            GUI.Label(new Rect(10, 10, 100, 10), text);

            var textAreaString = GUI.TextArea(new Rect(10, 30, 100, 10),
                CameraManager.i.GetWaypointTime().ToString());

            if(double.TryParse(textAreaString, out var result))
            {
                CameraManager.i.SetNewTime(result);
            }
        }
        private void DrawPanel()
        {
            GUI.color = Color.yellow;
            GUI.Box(new Rect(0, 0, 200, 200), "");
        }

        public void Toggle() { _enabled = !_enabled; }
    }
}
