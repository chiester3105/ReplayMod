using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NuclearOption.SceneLoading;
using ReplayMod.Events;
using ReplayMod.Events.ConcreteEvents;
using ReplayMod.Patches;
using ReplayMod.Patches.WeaponPatches;
using UnityEngine;

namespace ReplayMod.Core
{
    public class Recorder : MonoBehaviour
    {
        private void Start()
        {
            Plugin.logger.LogInfo("Recorder created");
            DontDestroyOnLoad(this);
        }
        private void OnEnable()
        {
            Subscribe();
        }
        private void OnDisable()
        {
            Unsubscribe();
        }
        private async Task OnDestroy()
        {
            await StopRecording();
        }
        private void Subscribe()
        {
            UnitRegistryPatch.onRegisterUnit += HandleUnitRegister;
            UnitRegistryPatch.onUnregisterUnit += HandleUnitUnregister;
            UnitPatch.onPartDetached += HandlePartDetach;
            AircraftPatch.onSetGear += HandleSetGear;
            MissilePatch.onDetonate += HandleMissileDetonate;

            WeaponManagerPatch.onWeaponManagerFire += HandleWeaponManagerFire;
            WeaponManagerPatch.onSetActiveStation += HandleSetActiveStation;
        }
        private  void Unsubscribe()
        {
            UnitRegistryPatch.onRegisterUnit -= HandleUnitRegister;
            UnitRegistryPatch.onUnregisterUnit -= HandleUnitUnregister;
            UnitPatch.onPartDetached -= HandlePartDetach;
            AircraftPatch.onSetGear -= HandleSetGear;
            MissilePatch.onDetonate -= HandleMissileDetonate;

            WeaponManagerPatch.onWeaponManagerFire -= HandleWeaponManagerFire;
            WeaponManagerPatch.onSetActiveStation -= HandleSetActiveStation;
        }

        private Dictionary<PersistentID, Unit> _unitsOnTrack = new();
        private Dictionary<PersistentID, List<Turret>> _turretsOnTrack = new();
        private List<string> _usedUnits = new();
        private double _virtualTime = 0;
        private void HandleUnitRegister(Unit unit, PersistentID id)
        {
            if (!_usedUnits.Contains(unit.definition.jsonKey)) _usedUnits.Add(unit.definition.jsonKey);

            if (id.Id == 0) return;
            if (_unitsOnTrack.TryGetValue(id, out var u))
            {
                Plugin.logger.LogError($"Collision detected:\n" +
                    $"Existing unit: {u}\n" +
                    $"Trying to add: {unit}");
            }
            else
            {
                _unitsOnTrack.Add(id, unit);
                _lastPositionUpdateTime.Add(id, _virtualTime);
                
                HandleWeaponStationAsync(unit).Forget();

                var writer = ReplayEventFactory.GetEvent<SpawnEvent>();
                writer.Time = _virtualTime;
                writer.unitId = unit.persistentID.Id;
                writer.jsonKey = unit.definition.jsonKey;

                if (unit.rb != null)
                    writer.startingVelocity = unit.rb.velocity;
                else
                    writer.startingVelocity = new Vector3(0, 0, 0);

                writer.rotation = unit.transform.rotation;
                writer.pos = unit.transform.position.ToGlobalPosition();

                if (unit.NetworkHQ != null)
                    writer.factionName = unit.NetworkHQ.faction.factionName;
                else
                    writer.factionName = "no faction";

                if (unit is Missile m)
                    writer.ownerId = m.ownerID.Id;
                else
                    writer.ownerId = unit.persistentID.Id;

                if (unit is Aircraft aircraft)
                {
                    writer.isAircraft = true;
                    writer.liveryId = aircraft.LiveryKey.Id;
                    writer.liveryIndex = aircraft.LiveryKey.Index;
                    writer.liveryType = aircraft.LiveryKey.Type;

                    if (aircraft.loadout.weapons == null) Plugin.logger.LogWarning("LOADOUT IS NULL");
                    writer.weapons = aircraft.loadout.weapons;
                }
                else
                    writer.isAircraft = false;

                _eventQueue.Add(writer);
                _eventCount++;
            }
        }

        private HashSet<uint> _despawnSkip = new();
        private void HandleUnitUnregister(Unit unit)
        {
            if (unit.persistentID.Id == 0) return; 
            if(_despawnSkip.Contains(unit.persistentID.Id))
            {
                _despawnSkip.Remove(unit.persistentID.Id);
                return;
            }
            _unitsOnTrack.Remove(unit.persistentID);
            _lastPositionUpdateTime.Remove(unit.persistentID);
            var writer = ReplayEventFactory.GetEvent<DespawnEvent>();
            writer.Time = _virtualTime;
            writer.unitId = unit.persistentID.Id;
            writer.isMissileDetonated = false;
            _turretsOnTrack.Remove(unit.persistentID);

            _eventQueue.Add(writer);
            _eventCount++;
        }
        private void HandleMissileDetonate(Missile missile)
        {
            _unitsOnTrack.Remove(missile.persistentID);
            _lastPositionUpdateTime.Remove(missile.persistentID);
            var writer = ReplayEventFactory.GetEvent<DespawnEvent>();
            writer.Time = _virtualTime;
            writer.unitId = missile.persistentID.Id;
            writer.isMissileDetonated = true;
            _eventQueue.Add(writer);
            _eventCount++;
            _despawnSkip.Add(missile.persistentID.Id);
        }
        private void HandlePartDetach(Unit unit, byte partId)
        {
            var writer = ReplayEventFactory.GetEvent<DetachPartEvent>();
            writer.Time = _virtualTime;
            writer.unitID = unit.persistentID.Id;
            writer.partID = partId;
            _eventQueue.Add(writer);
            _eventCount++;
        }
        private void HandleSetGear(Aircraft aircraft, bool deployed)
        {
            var writer = ReplayEventFactory.GetEvent<SetGearEvent>();
            writer.deployed = deployed;
            writer.Time = _virtualTime;
            writer.unitId = aircraft.persistentID.Id;
            _eventQueue.Add(writer);
            _eventCount++;
        }
        private async UniTask HandleWeaponStationAsync(Unit unit)
        {
            //await weapon station register, bc it is null even
            //after unit been registered
            await UniTask.WhenAny
                (UniTask.WaitUntil(() => unit.weaponStations != null),
                UniTask.WaitForSeconds(1));
            if (unit.weaponStations != null)
            {
                foreach (var ws in unit.weaponStations)
                {
                    foreach (var t in ws.Turrets)
                    {
                        if (_turretsOnTrack.TryGetValue(unit.persistentID, out var list))
                            list.Add(t);
                        else
                            _turretsOnTrack[unit.persistentID] = new List<Turret>() { t };
                    }
                }
            }
        }

        private void HandleSetActiveStation(WeaponManager manager, byte stationIdx)
        {
            var writer = ReplayEventFactory.GetEvent<SetActiveStationEvent>();
            writer.Time = _virtualTime;
            writer.unitId = manager.aircraft.persistentID.Id;
            writer.stationIdx = stationIdx;

            _eventQueue.Add(writer);
            _eventCount++;
        }

        private void HandleWeaponManagerFire(WeaponManager manager)
        {
            var writer = ReplayEventFactory.GetEvent<WeaponManagerFireEvent>();
            writer.Time = _virtualTime;
            writer.unitId = manager.aircraft.persistentID.Id;

            _eventQueue.Add(writer);
            _eventCount++;
        }
        private Dictionary<PersistentID, double> _lastPositionUpdateTime = new();
        private Dictionary<PersistentID, double> _lastInputUpdateTime = new();
        private Dictionary<PersistentID, double> _lastTurretUpdateTime = new();
        private void Update()
        {
            if (_cts == null || _cts.IsCancellationRequested) return;
            
            foreach (var kvp in _unitsOnTrack)
            {
                var pid = kvp.Key;
                Unit unit = kvp.Value;
                if (unit == null) continue; 
                if (!_lastPositionUpdateTime.TryGetValue(pid, out double lastTime))
                    continue;
                double interval = ReplayManager.i.GetIntervalForUnit(unit);
                if (_virtualTime - lastTime > interval)
                {
                    _lastPositionUpdateTime[pid] = _virtualTime;
                    var writer = ReplayEventFactory.GetEvent<UpdatePositionEvent>();
                    writer.position = unit.transform.position.ToGlobalPosition();
                    writer.rotation = unit.transform.rotation;
                    if(unit.rb != null)
                        writer.velocity = unit.rb.velocity;
                    writer.unitId = unit.persistentID.Id;
                    writer.Time = _virtualTime;
                    _eventQueue.Add(writer);
                    _eventCount++;    
                }
                if(unit is Aircraft aircraft)
                {
                    if (!_lastInputUpdateTime.TryGetValue(pid, out var lastUpdate))
                        _lastInputUpdateTime[pid] = _virtualTime;
                    if(_virtualTime - lastUpdate > 2)
                    {
                        var inputs = ReplayEventFactory.GetEvent<UpdateInputsEvent>();
                        inputs.CopyFromControlInputs(aircraft.controlInputs);
                        inputs.id = aircraft.persistentID.Id;
                        inputs.Time = _virtualTime;
                        _eventQueue.Add(inputs);
                        _eventCount++;
                        _lastInputUpdateTime[pid] = _virtualTime;
                        
                        //ill implement using compressed inputs from mirage later
                    }
                }
            }
            
            foreach(var kvp in _turretsOnTrack)
            {
                if (_lastTurretUpdateTime.TryGetValue(kvp.Key, out var lastUpdate))
                {
                    if (_virtualTime - _lastTurretUpdateTime[kvp.Key] > ReplayManager.TurretsUpdateInterval)
                    {
                        _lastTurretUpdateTime[kvp.Key] = _virtualTime;

                        foreach (var turret in kvp.Value)
                        {
                            var writer = ReplayEventFactory.GetEvent<UpdateTurretEvent>();

                            writer.Time = _virtualTime;
                            writer.turretIdx = turret.turretIndex;
                            writer.attachedUnitId = kvp.Key.Id;
                            writer.elevationAngle = turret.elevationAngle;
                            writer.traverseAngle = turret.traverseAngle;
                            writer.weaponStationIdx = turret.currentWeaponStation.Number;
                            _eventQueue.Add(writer);
                        }
                    }
                }
                else _lastTurretUpdateTime[kvp.Key] = _virtualTime;
            }
            _virtualTime += Time.deltaTime; 
        }

        private long _eventCount = 0;
        private double _duration = 0;
        private BlockingCollection<IReplayEvent> _eventQueue = new();  
        private Task _fileWriter;
        private CancellationTokenSource _cts;

        private BinaryWriter _binaryWriter;
        private FileStream _fileStream;
        public async Task StartRecording(string filePath, string mapPrefabName)
        {
            try
            {
                if (_fileWriter != null && !_fileWriter.IsCompleted)
                    await StopRecording();
                if(mapPrefabName == "UnknownMap")
                {
                    await ReplayManager.i.SwitchState(ModStates.Idle);
                    return;
                }
                _virtualTime = 0;
                _currentMap = mapPrefabName;
                _currentMapKey = MapSettingsManager.i.MapLoader.CurrentMap;
                //Subscribe();

                _eventQueue = new BlockingCollection<IReplayEvent>();
                Unit[] units = FindObjectsOfType<Unit>();
                Plugin.SafeLog($"Units {units.Length}");
                Queue<Unit> other = new();

                foreach (Unit unit in units) //missile owners have to spawn firstly
                {
                    if (!(unit is Missile))
                        HandleUnitRegister(unit, unit.persistentID);
                    else other.Enqueue(unit);
                }
                while (other.Count > 0)
                {
                    var u = other.Dequeue();
                    HandleUnitRegister(u, u.persistentID);
                }
                
                _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536);
                _binaryWriter = new BinaryWriter(_fileStream);
                _cts = new CancellationTokenSource();

                WriteHeader(_binaryWriter, _fileStream);
                Plugin.SafeLog($"Starting writeloop, starting spawns {_eventQueue.Count}");

                _fileWriter = Task.Run(() => WriteLoop(_cts.Token));

                Plugin.SafeLog($"recorder started, starting spawns {_eventQueue.Count}");
            }
            catch (Exception ex)
            {
                Plugin.logger.LogError(ex);
            }
        }

        public async Task StopRecording()
        {
            if (_cts == null) return;
            _duration = _virtualTime;
            _cts.Cancel();
            _eventQueue.CompleteAdding();
            try { await _fileWriter; } catch (OperationCanceledException) { }

            
            _fileStream.Seek(_durationPosition, SeekOrigin.Begin);
            _binaryWriter.Write(_duration);      
            _fileStream.Seek(_eventCountPosition, SeekOrigin.Begin);
            _binaryWriter.Write(_eventCount);

            
            _cts.Dispose();
            _fileWriter.Dispose();
            _binaryWriter?.Dispose();
            _fileStream?.Dispose();
            _eventQueue.Dispose();

            _unitsOnTrack.Clear();
            _lastPositionUpdateTime.Clear();
            
            
            _cts = null;
            _fileWriter = null;
            _binaryWriter = null;
            _fileStream = null;
            _eventQueue = null;
        }

        public async Task StopAndDestroy()
        {
            await StopRecording();
            Destroy(this);
        }
        private void WriteLoop(CancellationToken token)
        {
            try
            { 
                foreach(var ev in _eventQueue.GetConsumingEnumerable(token))
                {
                    _binaryWriter.Write((byte)ev.EventType);
                    ev.Write(_binaryWriter);
                    ReplayEventFactory.Return(ev);
                }
            }
            catch(OperationCanceledException)
            {
                Plugin.SafeLog("Write loop canceled");
            }
            catch(Exception ex)
            {
                Plugin.SafeLogError(ex.ToString());
            }
            finally
            {
                _binaryWriter?.Flush();
            }
        }

        private async void OnApplicationQuit()
        {
            UnityEngine.Debug.LogWarning("Saving replay bc game is closing");
            await StopAndDestroy();
        }

        private MapKey _currentMapKey;
        private string _currentMap;
        private long _durationPosition;
        private long _eventCountPosition;
        /// <summary>
        /// Header format:
        /// 1. random number as unique application marker [uint]
        /// 2. version [major + minor + patch (3 bytes)]
        /// 3. map prefab name [string]
        /// 4. start time (real time) [long]
        /// 5. duration [double]
        /// 6. event count [long]
        /// 7. count of used units
        /// 8. used units
        /// </summary>
        /// <param name="bw"></param>
        private void WriteHeader(BinaryWriter bw, FileStream fs)
        {
            //file info
            bw.Write(Plugin.AppId);
            bw.Write(Plugin.Major);
            bw.Write(Plugin.Minor);
            bw.Write(Plugin.Patch);

            //map section
            bw.Write((byte)_currentMapKey.Type);
            bw.Write(_currentMapKey.Path);

            //datetime (to sort replays in gui in the future)
            bw.Write(DateTime.UtcNow.Ticks);

            //reserved bytes section
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((byte)0);

            //details
            _durationPosition = fs.Position;
            bw.Write((double)0);
            _eventCountPosition = fs.Position;
            bw.Write((long)0);
            ushort usedUnits = (ushort)_usedUnits.Count;
            bw.Write(usedUnits);
            foreach(var key in _usedUnits)
            {
                bw.Write(key);
            }
        }
    }
}
