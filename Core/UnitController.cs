using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NuclearOption.Jobs;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using ReplayMod.Data;
using ReplayMod.Events.ConcreteEvents;
using UnityEngine;

using Object = UnityEngine.Object;
namespace ReplayMod.Core
{
    public class UnitController
    {
        private Dictionary<uint, PersistentID> _unitMap = new();
        private Dictionary<PersistentID, uint> _reverseUnitMap = new();
        private Dictionary<uint, double> _lastFrameTime = new();
        
        private Dictionary<PersistentID, bool> _isRbDisabled = new();
        private MapKey _currentMap;
        public UnitController(MapKey key)
        {
            _currentMap = key;
        }
        public void SpawnUnit(SpawnEvent e)
        {
            try
            {
                
                //Plugin.logger.LogInfo('1');
                if (!Encyclopedia.Lookup.TryGetValue(e.jsonKey, out var definition))
                    Plugin.logger.LogWarning("cant spawn unit");
                Plugin.logger.LogInfo($"trying to spawn {e.jsonKey}");
                Unit u = null;
                if (NetworkSceneSingleton<Spawner>.i == null)
                {
                    Plugin.logger.LogError("current Spawner is null");
                    return;
                }
                
                var hq = FactionRegistry.HqFromName(e.factionName);
                if(hq == null)
                {
                    Plugin.logger.LogInfo("hq is null");
                    return;
                }

                switch (definition)
                {
                    case VehicleDefinition def:
                        {
                            u = SpawnVehicle(e, hq, def);
                            break;
                        }
                    case MissileDefinition def:
                        {
                            u = SpawnMissile(e, hq, def);
                            break;
                        }
                    case AircraftDefinition def:
                        {
                            u = SpawnAircraft(e, hq, def);
                            break;
                        }
                    case ShipDefinition def:
                        {
                            u = SpawnShip(e, hq, def);
                            break;
                        }
                    case BuildingDefinition def:
                        {
                            u = SpawnBuilding(e, hq, def);
                            break;
                        }
                    case SceneryDefinition def:
                        {
                            u = SpawnScenery(e, hq, def);
                            break;
                        }
                    default:
                        {
                            u = SpawnPilotOrContainer(e, hq, definition);
                            break;
                        }
                }
                if (u != null)
                {
                    _unitMap[e.unitId] = u.persistentID;
                    _reverseUnitMap[u.persistentID] = e.unitId;
                    _isRbDisabled[u.persistentID] = false;
                }
                
            }
            catch (Exception ex)
            {
                Plugin.logger.LogError($"Spawn exception, spawnEvent params: {e.jsonKey} {ex}");
                if (e.jsonKey == null) Plugin.SafeLogWarning("json key null");
                if (e.startingVelocity == null) Plugin.SafeLogWarning("sv null");
                if (e.factionName == null) Plugin.SafeLogWarning("faction name null");
                if (e.pos == null) Plugin.SafeLogWarning("pos null");
                if (e.rotation == null) Plugin.SafeLogWarning("rotation null");
                throw ex;
            }
           
        }

        private Unit SpawnVehicle(SpawnEvent e, FactionHQ hq, VehicleDefinition def)
        {
            var u = NetworkSceneSingleton<Spawner>.i.SpawnVehicle(def.unitPrefab, e.pos, e.rotation, e.startingVelocity, hq, null, 1, false, null);
            
           // DisableJobs(u); 
           // DisablePhysics(u);
            return u;
        }
        private Unit SpawnAircraft(SpawnEvent e, FactionHQ hq, AircraftDefinition def)
        {
            var key = new LiveryKey(e.liveryType, e.liveryIndex, e.liveryId.ToString());
            Loadout loadout = new() { weapons = e.weapons };
            return NetworkSceneSingleton<Spawner>.i.SpawnAircraft(null, def.unitPrefab, loadout, 1, key, e.pos, e.rotation, e.startingVelocity, null, hq, null, 1, 1);
        }
        private Unit SpawnMissile(SpawnEvent e, FactionHQ hq, MissileDefinition def)
        {
            Plugin.logger.LogInfo($"Trying to spawn missile");
            TrySearchUnit(e.ownerId, out var pid, out var owner);
            return NetworkSceneSingleton<Spawner>.i.SpawnMissile(def, e.pos.ToLocalPosition(), e.rotation, e.startingVelocity, null, owner);  
        }

        
        private Unit SpawnShip(SpawnEvent e, FactionHQ hq, ShipDefinition def)
        {
            Plugin.logger.LogInfo($"Trying to spawn {def.unitName}, key {e.jsonKey}, type {def.shipType}");
            
            return NetworkSceneSingleton<Spawner>.i.SpawnShip(def.unitPrefab, e.pos, e.rotation, hq, GetUniqueName(), 1, false); 
        }
        private Unit SpawnBuilding(SpawnEvent e, FactionHQ hq, BuildingDefinition def)
        {
            return NetworkSceneSingleton<Spawner>.i.SpawnBuilding(def.unitPrefab, e.pos, e.rotation, hq, null, null, false, null);
        }
        private Unit SpawnScenery(SpawnEvent e, FactionHQ hq, SceneryDefinition def)
        {
            return NetworkSceneSingleton<Spawner>.i.SpawnScenery(def.unitPrefab, e.pos, e.rotation, null);
        }
        private Unit SpawnPilotOrContainer(SpawnEvent e, FactionHQ hq, UnitDefinition def)
        {    
            if (def.code == "PILOT")
            {
                if (ReplayManager.i.IgnorePilotsSpawn) return null;
                return NetworkSceneSingleton<Spawner>.i.SpawnPilot(def.unitPrefab, e.pos, e.rotation, hq, null);               
            }
            return NetworkSceneSingleton<Spawner>.i.SpawnContainer(def.unitPrefab, e.pos, e.rotation, hq, null);           
        }
        public bool TrySearchUnit(uint id, out PersistentID pid, out Unit unit)
        {
            pid = default;
            unit = null;
            if (!_unitMap.TryGetValue(id, out pid))
            {
                return false;
            }
            if (!pid.TryGetUnit(out unit) || unit == null)
            {
                return false;
            }
            return true;
        }
        public void MoveUnit(uint id, List<PositionSnapshot> positions, double currentTime)
        {
            if (!TrySearchUnit(id, out var pid, out var unit)) return;
            if (positions == null || positions.Count == 0)
                return;

            if (!_isRbDisabled[unit.NetworkpersistentID] && unit is Aircraft a)
            {
                DisableRb(a);
            }

            // key frames search
            int idx = Tools.FindLastIndex(positions, currentTime);
            if (idx < 0) 
            {
                SetUnitTransform(unit, positions[0]);
                return;
            }
            if (idx >= positions.Count - 1)
            {
                SetUnitTransform(unit, positions[positions.Count - 1]);
                return;
            }

            var prev = positions[idx];
            var next = positions[idx + 1];
            float t = (float)((currentTime - prev.time) / (next.time - prev.time));
            t = Mathf.Clamp01(t);

            // interpolation gp -> vector3
            Vector3 pos = Vector3.Lerp(prev.position.ToLocalPosition(), next.position.ToLocalPosition(), t);
            Quaternion rot = Quaternion.Slerp(prev.rotation, next.rotation, t);
            Vector3 vel = Vector3.Lerp(prev.velocity, next.velocity, t);
            unit.transform.SetPositionAndRotation(pos, rot);
            if(unit.rb != null && !unit.rb.isKinematic) unit.rb.velocity = vel;
            

        }

        private void SetUnitTransform(Unit unit, PositionSnapshot snapshot)
        {
            //Plugin.logger.LogInfo($"Trying to set position: {unit} {snapshot.position}");
            unit.transform.SetPositionAndRotation(snapshot.position.ToLocalPosition(), snapshot.rotation);
            if(unit.rb != null && !unit.rb.isKinematic)
                unit.rb.velocity = snapshot.velocity;
        }

        private void DisableRb(Aircraft aircraft)
        {
            aircraft.SetSimplePhysics();
            /*foreach (var part in aircraft.partLookup)
            {
                var colliders = part.GetComponentsInChildren<Collider>(true);
                foreach (var col in colliders)
                {
                    col.enabled = false;

                }
                var rb = part.rb;
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.detectCollisions = false;
                }
            }*/
        }

        private void EnableRb(Aircraft aircraft)
        {
            aircraft.SetComplexPhysics();
            /*foreach (var part in aircraft.partLookup)
            {
                var colliders = part.GetComponentsInChildren<Collider>(true);
                foreach (var col in colliders)
                {
                    col.enabled = true;

                }
                var rb = part.rb;
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.detectCollisions = true;

                }
            }
            */
        }

        public async UniTask ResetWorld()
        {
            try
            {
                await UniTask.SwitchToMainThread();
                Unit[] onScene = Object.FindObjectsOfType<Unit>();
                foreach (var u in onScene)
                {
                    u.transform.position = (new GlobalPosition(-40000, -40000, -40000)).ToLocalPosition();
                    if (_isRbDisabled.TryGetValue(u.persistentID, out var disabled)
                        && disabled && u is Aircraft a)
                    {
                        EnableRb(a);
                    }
                    if(u is GroundVehicle vehicle)
                    {  
                        vehicle.wreckage = null;                        
                    }
                    else if(u is Building building)
                    {
                        building.wreckage = null;
                    }
                    _pidsOnDestroy.Add(u.persistentID.Id);
                    Object.Destroy(u);
                    _turretSnapshots.Clear();
                }
                Plugin.logger.LogInfo("Reset finished");
            }
            catch(Exception e)
            {
                UnityEngine.Debug.LogError($"Reset world failed: {e}");
            }
        }

        public void ApplyInputs(UpdateInputsEvent e)
        {
            if (!TrySearchUnit(e.id, out var pid, out var unit) || !(unit is Aircraft a)) return;
            e.CopyToControlInputs(a.controlInputs);           
        }
        public void DetachPart(DetachPartEvent e)
        {
            if (!TrySearchUnit(e.unitID, out var pid, out var unit)) return;

            var partLookup = unit.partLookup;
            var part = partLookup.FirstOrDefault(p => p.id == e.partID);
            if (part != null)
            {
                if (part is AeroPart aeroPart)
                {
                    aeroPart.CreateRB(default, aeroPart.transform.position);
                }
                Vector3 relativePos = unit.transform.InverseTransformPoint(part.transform.position);
                unit.DetachPart(part.id, unit.rb.velocity, relativePos);
            }
        }

        public void DespawnUnit(uint unitId)
        {
            if(!TrySearchUnit(unitId, out var pid, out var unit)) return;
            if(unit is Missile m)
            {
                m.Detonate(m.rb.velocity, false, false);
                return;
            }
            Object.Destroy(unit);
        }

        public void SetGear(SetGearEvent e)
        {
            if (!TrySearchUnit(e.unitId, out var pid, out var unit) || !(unit is Aircraft a)) return;
            a.SetGear(e.deployed);
        }

        private void DisablePhysics(Unit unit)
        {
            if (unit == null) return;
            var rb = unit.rb;
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        private void DisableJobs(GroundVehicle unit)
        {
            JobManager.Remove(ref unit.JobPart);
            GroundVehicle.DisposeJobFields(ref unit.JobFields);           
        }

        private HashSet<uint> _pidsOnDestroy = new();
        public bool ShouldContinuePatchedMethod(uint id)
        {
            if (_pidsOnDestroy.Contains(id))
            {
                return false;
            }
            return true;
        }

        // without that units with attached airbase (carriers, dynamo, fregat)
        // cant spawn correctly
        private int _generated = 0;
        private string GetUniqueName()
        {
            return $"Unit N{_generated++}";
        }


        private Dictionary<Turret, List<TurretSnapshot>> _turretSnapshots = new();
        public void AddTurretSnapshot(UpdateTurretTransform e)
        {
            try
            {
                TurretSnapshot snapshot = TurretSnapshot.Create(e);
                if (!TrySearchUnit(e.attachedUnitId, out var pid, out var unit)) return;
                if (e.turretIdx > unit.weaponStations.Count - 1) return;
                var turret = unit.weaponStations[e.weaponStationIdx].GetTurret();
                //Plugin.logger.LogInfo($"ADD TURRET SNAPSHOT: {turret} | {turret.attachedUnit}");
                if (turret != null && _turretSnapshots.TryGetValue(turret, out var list))
                {
                    //Plugin.logger.LogInfo("Adding snapshot");
                    list.Add(snapshot);
                }
                else
                {
                    _turretSnapshots[turret] = new List<TurretSnapshot> { snapshot };
                    turret.enabled = false;
                    //Plugin.logger.LogInfo("Created snapshot list");
                }
            }
            catch (Exception ex)
            {
                Plugin.logger.LogWarning($"Add turret snapshot error: {ex}");
            }
        }
        public void MoveTurrets(double time)
        {
           //Plugin.logger.LogInfo($"Trying to move turrets: {_turretSnapshots.Count}");
            foreach(var kvp  in _turretSnapshots)
            {
                var positions = kvp.Value;
                if(positions == null || positions.Count == 0) continue;
                var turret = kvp.Key;
                if (turret == null) continue;
                int idx = Tools.FindLastIndex(positions, time);
                if (idx < 0)
                {
 
                    SetTurretTransform(turret, positions[0].elevationAngle, positions[0].traverseAngle);
                    return;
                }
                if (idx >= positions.Count - 1)
                {

                    SetTurretTransform(turret, positions[positions.Count - 1].elevationAngle, positions[positions.Count - 1].traverseAngle);
                    return;
                }

                var prev = positions[idx];
                var next = positions[idx + 1];
                float t = (float)((time - prev.time) / (next.time - prev.time));
                t = Mathf.Clamp01(t);

                // interpolation gp -> vector3
                float elevationAngle = Mathf.Lerp(prev.elevationAngle, next.elevationAngle, t);
                float traverseAngle = Mathf.Lerp(prev.traverseAngle, next.traverseAngle, t);

                SetTurretTransform(turret, elevationAngle, traverseAngle);

                if(idx > 10) positions.RemoveRange(0, idx-1);
            }
        }

        private void SetTurretTransform(Turret turret, float elevation, float traverse)
        {
            
            turret.elevationTransform.localEulerAngles = new Vector3(elevation, 0f, 0f);
            turret.transform.localEulerAngles = new Vector3(0f, traverse, 0f);

            turret.elevationAngle = elevation;
            turret.traverseAngle = traverse;
           // Plugin.logger.LogInfo($"Update turret tranform for turret {turret}, attached {turret.attachedUnit}\n" +
           //     $"entered elevation and traverse: {elevation}; {traverse}|\n" +
           //     $"current: {turret.elevationAngle}; {turret.traverseAngle}");
        }
    }
}
