using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using ReplayMod.Data;
using UnityEngine;

namespace ReplayMod.Core
{
    public class ReplayManager : MonoBehaviour
    {
        public static ReplayManager i {  get; private set; }

        public double virtualTime { get; set; } = 0;

        public const double AircraftUpdateInterval = 0.05; // aircrafts and missiles actually
        public const double GroundVehicleUpdateInterval = 0.4; //grounds and ships
        public const double BuildingUpdateInterval = 30; //lmao
        public const double UpdateInputsInterval = 0.3;
        public const double TurretsUpdateInterval = 1.5;  

        private ModStates _currentState;
        private Recorder _recorder;
        private Player _player;
        private UI _mainPanel;
        public bool UseCaching { get; private set; } = false;
        public bool IgnorePilotsSpawn { get; private set; } = false;
        public double WorldSnapshotsInterval { get; private set; } = 20; 

        private string _lastFile = string.Empty;
        private string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Replays");
        public void Configure()
        {

        }

        private void Awake()
        {
            
        }
        public void Start()
        {
            try
            {
                
                if (i != null)
                {
                    Destroy(gameObject);
                    return;
                }

                i = this;
                DontDestroyOnLoad(gameObject);

                _mainPanel = gameObject.AddComponent<UI>();
                

                _recorder = gameObject.AddComponent<Recorder>();
                _player = gameObject.AddComponent<Player>();
                _recorder.enabled = false;
                _player.enabled = false;

                _currentState = ModStates.Idle;
                Plugin.logger.LogInfo("Replay manager initialized");
            }
            catch (Exception e)
            {
                Plugin.logger.LogError(e);
            }
        }
        public void Update()
        {
            
            HandleInputs();
            
                
        }

        public double GetIntervalForUnit(Unit unit)
        {
            switch (unit)
            {
                case Aircraft: return AircraftUpdateInterval;
                case Missile: return AircraftUpdateInterval;

                case GroundVehicle: return GroundVehicleUpdateInterval;
                case Ship: return GroundVehicleUpdateInterval;

                case Building: return BuildingUpdateInterval;

                default: return 0.2;
            }
            
        }

        
        public async Task SwitchState(ModStates newState)
        {
            if (newState == _currentState) return;
            var temp = _currentState;
            _currentState = newState;
            switch (temp) // correctly exit current state
            {
                case ModStates.Record:
                {
                    await _recorder.StopRecording();
                        _recorder.enabled = false;
                    break;
                }
                case ModStates.Replay:
                {
                   await _player.StopPlayingAndDestroy();
                        break;
                }
            }

            switch (newState) // enter new state
            {
                case ModStates.Record:
                    {
                        _ = _recorder.StopAndDestroy();
                        _recorder = gameObject.AddComponent<Recorder>();
                        _recorder.enabled = true;
                        _lastFile = GetFilePath();
                        Plugin.logger.LogInfo("Starting record");
                        await _recorder.StartRecording(_lastFile, GetMapPrefabName());
                        virtualTime = 0;
                        break;   
                    }
                case ModStates.Replay:
                    {
                        if ((NetworkManagerNuclearOption.i.Client != null
                            && NetworkManagerNuclearOption.i.Client.Active)
                            || (NetworkManagerNuclearOption.i.Server != null
                            && NetworkManagerNuclearOption.i.Server.Active))
                        {
                            if (SceneSingleton<GameplayUI>.i != null)
                            {
                                SceneSingleton<GameplayUI>.i.ResumeGame();
                            }
                            await NetworkManagerNuclearOption.i.StopAsync(true);
                        }
                        TimeScaleManager.Scale = 0;
                       
                        await _player.StopPlayingAndDestroy();
                       
                        _player = gameObject.AddComponent<Player>();
                        
                        DontDestroyOnLoad(_player);
                        
                        _player.enabled = true;
                        await _player.StartPlaying(_lastFile);
                        break;
                    }
                case ModStates.Idle:
                    {
                        _recorder.enabled = false;
                        _player.enabled = false;
                        break;
                    }
            }

           
        }
        /// <summary>
        /// Returns path from MapKey.Path
        /// </summary>
        /// <returns></returns>
        private string GetMapPrefabName()
        {
            string mapName = "UnknownMap";
            if (MapSettingsManager.i?.MapLoader?.CurrentMap != null)
                mapName = MapSettingsManager.i.MapLoader.CurrentMap.Path;
            else
                Plugin.logger.LogWarning("Current map not available, using 'UnknownMap'");
            return mapName;
        }
        private string GetFilePath()
        {
            string domain = AppDomain.CurrentDomain.BaseDirectory;
            string replayDir = Path.Combine(domain, "Replays");
            if (!Directory.Exists(replayDir))
                Directory.CreateDirectory(replayDir);

            string date = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var mapName = GetMapPrefabName();

            string fileName = $"{mapName}_{date}.replay";
            
            foreach (char c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');
            return Path.Combine(replayDir, fileName);
        }

        
        private void HandleInputs()
        {
            if(Input.GetKeyDown(KeyCode.I))
            {
                if (_currentState != ModStates.Record)
                    SwitchState(ModStates.Record);
                else
                    SwitchState(ModStates.Idle);  
            }
            /*if (Input.GetKeyDown(KeyCode.RightShift))
            {
                if (_currentState != ModStates.Replay)
                    SwitchState(ModStates.Replay);
                else
                    SwitchState(ModStates.Idle);
            }*/
            if(Input.GetKeyDown(KeyCode.Space))
            {
                if (_currentState == ModStates.Replay && _player != null)
                {
                    _player.TogglePause();
                }
            }
            if(Input.GetKeyDown(KeyCode.F5))
            {
                _mainPanel.Toggle();
            }
        }
        
        public bool ShouldContinue(uint id)
        {
            if (_player == null || _currentState != ModStates.Replay) return true;
            return _player.ShouldContinue(id);
        }

        public ModStates GetState() { return _currentState; }

        public string[] GetFiles()
        {
            if(!Directory.Exists(_path)) Directory.CreateDirectory(_path);
            return Directory.EnumerateFiles(_path).Where(s =>
            {
                var subString = s.Split('.');
                return subString[subString.Length - 1] == "replay";
            }).ToArray();
        }

        public void TimelineJump(double time)
        {
            if(_player != null && _player.enabled == true && _currentState == ModStates.Replay)
            {
                _player.TimelineJump(time).Forget();
            }
        }

        public bool TryGetReplayInfo(string fileName, out Header header)
        {
            try
            {
                string fullPath = Path.Combine(_path, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        header = Header.Read(reader);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.logger.LogInfo($"Exception while reading header:\n{ex}");
                header = default;
                return false; 
            }
        }

        public async UniTask StartPlaying(string fileName)
        {
            _lastFile = Path.Combine(_path, fileName);
            await SwitchState(ModStates.Idle);
            _ = SwitchState(ModStates.Replay);
        }
        public double GetDuration()
        {
            if (_player != null)
            {
                return _player.replayDuration;
            }
            return -1;
        }
        public double GetCurrentVirtualTime()
        {
            if(_player != null )
            {
                return _player.currentVirtualTime;
            }
            return -1;
        }

        public void TogglePause()
        {
            _player.TogglePause();
        }
    }

    
}
