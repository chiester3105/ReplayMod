using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using NuclearOption.SceneLoading;
using ReplayMod.Data;
using ReplayMod.Events;
using ReplayMod.Events.ConcreteEvents;
using ReplayMod.Misc;
using UnityEngine;
using EventType = ReplayMod.Events.EventType;
namespace ReplayMod.Core
{
    public class Player : MonoBehaviour
    {
        private string _path;
        private MapKey _key;
        public double replayDuration { get; private set; }
        private long _eventsCount;
        private long _ticks;
        private long _dataStartOffset;

        private UnitController _unitCotroller;
        public double currentVirtualTime { get; private set; } = 0;

        private FileStream _fileStream;
        private BinaryReader _binaryReader;
        private CancellationTokenSource _producerCts;
        private CancellationTokenSource _cachingCts;
        private Task _producerTask;
        private BoundedEventBuffer _buffer;

        private bool _onReset = false;
        private bool _loaded = false;

        private void Awake()
        {
            DontDestroyOnLoad(this);
            GameManager.OnGameStateChanged.AddListener(HandleGameStateChange);
        }
        private void HandleGameStateChange()
        {
           // if (_onReset) return;
           // _ = StopPlayingAndDestroy();
        }
        public async Task StartPlaying(string filePath)
        {
            enabled = false;
            if (!File.Exists(filePath)) throw new FileNotFoundException($"No such file: {filePath}");

            _path = filePath;
            if (_fileStream != null) await StopPlaying();

            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
            _binaryReader = new BinaryReader(_fileStream);
            _producerCts = new CancellationTokenSource();
            _unitCotroller = new UnitController(_key);
            _buffer = new BoundedEventBuffer(ReplayManager.PlayerBoundedCapacity);

            await StartLoading(_binaryReader);

          
            if (!_loaded)
            {
                await ReplayManager.i.SwitchState(ModStates.Idle);
                throw new Exception("Loading failed, switching to idle state");
            }
            await ResetWorld();
            TimeScaleManager.Scale = 0;
            Plugin.DebugLog("world reset, awaiting afterload");
            await AfterLoad();
            Plugin.DebugLog("start playing finished");
            enabled = true;
        }
        
        
        /// <summary>
        /// Starts producer loop + awaits index builder
        /// </summary>
        /// <returns></returns>
        private async Task AfterLoad()
        {          
            foreach(var f in FactionRegistry.HQLookup)
            {
                f.Value.preventJoin = true;
            }
            if (ReplayManager.i.UseCaching)
            {
                _cachingCts = new CancellationTokenSource();
                _ = Task.Run(() => BuildCache(_cachingCts.Token)); // i dont need to wait it, fire and forget
            }

            Plugin.DebugLog("starting afterload");
            _producerTask = Task.Run(() => ProducerLoop(_producerCts.Token, _dataStartOffset));
            await Task.Run(() => BuildIndex()); // await bc its neccessary for timeline jumps
        }
        /// <summary>
        /// Reads header and sets _dataStartOffset pointer after header (on the first event)
        /// Also checks all resoursec used in replay and throws exception if resources are missing.
        /// </summary>
        /// <param name="br"></param>
        /// 
        /// <returns></returns>
        private async Task StartLoading(BinaryReader br)
        {
            try
            {
                //reading header
                int AppId = br.ReadInt32();
                if (AppId != Plugin.AppId)
                {
                    Plugin.logger.LogError("Trying to read unknown file format");
                    throw new IOException("Unknown file format");
                }

                byte Major = br.ReadByte();
                byte Minor = br.ReadByte();
                byte Patch = br.ReadByte();
                //ill add version compatability check later

                if(Major != Plugin.Major)
                {
                    Plugin.logger.LogError("Version mismatch, aborting file read");
                    return;
                }

                MapKey.KeyType keyType = (MapKey.KeyType)br.ReadByte();
                string path = br.ReadString();
                _key = new MapKey(keyType, path);

                _ticks = br.ReadInt64();

                //skip reserved bytes in current version
                br.ReadByte();// probably this will be bool flag for coordinates delta compression
                br.ReadByte();
                br.ReadByte();
                br.ReadByte();

                replayDuration = br.ReadDouble();
                _eventsCount = br.ReadInt64();
                //_indexStartOffset = br.ReadInt64();

                ushort usedUnits = br.ReadUInt16();
                string missingResources = string.Empty;
                for (int i = 0; i < usedUnits; i++)
                {
                    string key = br.ReadString();

                    if (!Encyclopedia.Lookup.ContainsKey(key))
                    {
                        missingResources = $"{missingResources}\n{key}";
                    }
                }
                if (!string.IsNullOrEmpty(missingResources))
                {
                    throw new Exception($"Failed to load replay, missing resources:" +
                        $"{missingResources}");
                }
                _dataStartOffset = _fileStream.Position;

                Plugin.logger.LogInfo("trying to invoke maploader");
                _loaded = await SceneHelper.LoadSceneAsync(_key, Path.GetFileName(_path));
            }
            catch (Exception ex)
            {
                Plugin.SafeLogError($"loading exception: {ex}");
            }
        }
        
        private ConcurrentDictionary<uint, List<PositionSnapshot>> _unitSnapshots = new();
        /// <summary>
        /// Reads events from file. ReadOffset points on file offset.
        /// To read from beginning readOffset should be equal _dataStartOffset
        /// </summary>
        /// <param name="token"></param>
        /// <param name="readOffset"></param>
        private void ProducerLoop(CancellationToken token, long readOffset)
        {
            try
            {
                _fileStream.Seek(readOffset, SeekOrigin.Begin);
                while (_fileStream.Position < _fileStream.Length
                    && !token.IsCancellationRequested)
                {
                   
                    EventType eventType = (EventType)_binaryReader.ReadByte();
                    var reader = ReplayEventFactory.CreateAndRead(eventType, _binaryReader);

                    _buffer.Add(reader, token);

                    token.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                Plugin.SafeLog("Producer task loop cancelled");
            }
            catch (Exception e)
            {
                Plugin.SafeLogError($"Exception while reading file:\n{e}");
            }
            finally
            {
                Plugin.SafeLog("Producer task loop finished");
            }
        }
        public async Task StopPlaying()
        {
            _producerCts?.Cancel();

            try { await _producerTask; } catch (Exception e) { }

            _binaryReader?.Close();
            _binaryReader?.Dispose();
            _fileStream?.Close();
            _fileStream?.Dispose();
            _cachingCts?.Cancel();
        }
        public async Task StopPlayingAndDestroy()
        {
            await StopPlaying();
            UnityEngine.Object.Destroy(this);
        }
        public void StopCachingProcess()
        {
            _cachingCts?.Cancel();
            _cachingCts?.Dispose();
            _cachingCts = null;
        }
        /// <summary>
        /// cache for restoring world state if user jumps on timeline.
        /// If disabled, will read all unit spawns and last coordinates
        /// From beginnig till file offset.
        /// If enabled, timeline jump will search nearest world snapshot
        /// with less time, will copy spawns and coordinates from there +
        /// will read other information part till file offset.
        /// </summary>
        private List<WorldSnapshot> _worldSnapshots = new List<WorldSnapshot>(); 
        
        // if user started replay withouth cache option and enabled it in process
        /// <summary>
        /// Builds timeline cache
        /// </summary>
        public void BuildCache()
        {
            _cachingCts?.Cancel();
            _cachingCts.Dispose();
            _cachingCts = new();
            _ = Task.Run(() => BuildCache(_cachingCts.Token));
        }
        private double[] _worldSnapshotTimes;
        /// <summary>
        /// builds cache by saving world states
        /// </summary>
        /// <param name="token"></param>
        private void BuildCache(CancellationToken token)
        {
            var interval = ReplayManager.i.WorldSnapshotsInterval;
            double lastSaved = -interval;          

            FileStream tempFS = new(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
            tempFS.Seek(_dataStartOffset, SeekOrigin.Begin);
            BinaryReader tempBR = new BinaryReader(tempFS);

            Dictionary<uint, SpawnSnapshot> spawns = new();
            Dictionary<uint, PositionSnapshot> lastPositions = new();
            _worldSnapshots = new();
            try
            {
                while (tempFS.Position < tempFS.Length)
                {
                    var type = (EventType)tempBR.ReadByte();
                    var e = ReplayEventFactory.CreateAndRead(type, tempBR);

                    if(e is SpawnEvent se)
                    {
                        spawns[se.unitId] = SpawnSnapshot.Create(se);
                    }
                    else if (e is DespawnEvent de)
                    {
                        spawns.Remove(de.unitId);
                        lastPositions.Remove(de.unitId);
                    }
                    else if (e is UpdatePositionEvent upe)
                    {
                        lastPositions[upe.unitId] = PositionSnapshot.Create(upe);
                    }

                    if (e.Time -  lastSaved > interval)
                    {
                        _worldSnapshots.Add(WorldSnapshot.Create(e.Time, spawns, lastPositions, tempFS.Position));
                        lastSaved = e.Time;
                        Plugin.SafeLog($"Added world snapshot for time {lastSaved}");
                    }

                    ReplayEventFactory.Return(e);
                }
            }
            catch (EndOfStreamException e)
            {
                //how?
                Plugin.SafeLogWarning($"{e.Message}\n{e.StackTrace}");
            }
            catch (Exception e)
            {
                Plugin.SafeLogError($"{e.Message}\n{e.StackTrace}");
            }
            finally
            {
                _worldSnapshotTimes = _worldSnapshots.Select(i=> i.time).ToArray();

                tempBR.Close();
                tempBR.Dispose();
                tempFS.Close();
                tempFS.Dispose();
                Plugin.SafeLog($"Cache built! Snapshots count: {_worldSnapshots.Count}");
            }
        }

        //cache for timeline jumps, probably i wont let the user configure it
        private List<Data.Index> _index = new List<Data.Index>();
        private double[] _indexTimes;
        /// <summary>
        /// builds index. Index uses to link time to file offset.
        /// Offset points on the first event with this time.
        /// </summary>
        private void BuildIndex()
        {
            using (FileStream tempFS = new(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
            {
                using (BinaryReader tempBR = new BinaryReader(tempFS))
                {
                    tempFS.Seek(_dataStartOffset, SeekOrigin.Begin);

                    double lastTime = -1;
                    try
                    {
                        while (tempFS.Position < tempFS.Length)
                        {
                            try
                            {
                                var position = tempFS.Position;
                                var type = (EventType)tempBR.ReadByte();
                                var reader = ReplayEventFactory.CreateAndRead(type, tempBR);

                                if (reader.Time > lastTime)
                                {
                                    var idx = new Data.Index
                                    {
                                        VirtualTime = reader.Time,
                                        Offset = position,
                                    };
                                    _index.Add(idx);
                                }
                                lastTime = reader.Time;
                                ReplayEventFactory.Return(reader);
                            }
                            catch (Exception ex)
                            {
                                Plugin.logger.LogInfo(ex);
                                //idk how to skip to next packet lmao
                                SkipToNextValid(tempBR, tempFS);
                            }
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        Plugin.SafeLog("Index reading finished");
                    }
                    catch (Exception e)//wtf
                    {
                        Plugin.SafeLogError($"Error while building index:\n{e}");
                    }
                }
            }
            _indexTimes = _index.Select(i => i.VirtualTime).ToArray();
            Plugin.SafeLog("Index built");
        }
        private void SkipToNextValid(BinaryReader br, FileStream fs)
        {
            var position = fs.Position;    
        }
        public async UniTask TimelineJump(double targetTime)
        {
            _producerCts.Cancel();
            try { await _producerTask; } catch (OperationCanceledException) { }

            _producerCts.Dispose();
            _producerCts = new();

            if (SceneSingleton<GameplayUI>.i != null)
            {
                SceneSingleton<GameplayUI>.i.ResumeGame();
            }
            // reload scene + place camera to pos before reloading
            _onReset = true;
            CameraStateManager.i.GetCameraPosition(out var pos);
            
            await NetworkManagerNuclearOption.i.StopAsync(true);
            await SceneHelper.LoadSceneAsync(_key, Path.GetFileName(_path));
            

            var fileOffset = CalculateOffset(targetTime);

            await UniTask.SwitchToMainThread();
            await ResetWorld();
            TimeScaleManager.Scale = 0;
            RestoreWorldState(targetTime, fileOffset);

            CameraStateManager.i.SetCameraPosition(pos);

            currentVirtualTime = targetTime;
            _producerTask = Task.Run(() => ProducerLoop(_producerCts.Token, fileOffset));
            _onReset = false;
        }

        private int SearchNearestLessIndex(double[] values, double targetValue)
        {
            if (values.Length == 0) return -1;
            if (targetValue < values[0]) return -1;

            int left = 0;
            int right = values.Length - 1;
            int result = -1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (values[mid] <= targetValue)
                {
                    result = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            return result;
        }

        private long CalculateOffset(double targetTime)
        {
            int idx = SearchNearestLessIndex(_indexTimes, targetTime);
            if (idx >= 0) return _index[idx].Offset;
            else return _dataStartOffset;
        }

        private async UniTask ResetWorld()
        {  
            await _unitCotroller.ResetWorld();
            _buffer.ClearAndReturnToPool();
            _unitSnapshots.Clear();
            _restoreBuffer.Clear();
            ReplayManager.i.onReset?.Invoke();
        }
        private void RestoreWorldState(double time, long producerStartPos)
        {
            _restoreBuffer.Clear();
            Dictionary<uint, SpawnEvent> spawns = new();
            Dictionary<uint, PositionSnapshot> moves = new();

            if (_worldSnapshots.Count > 0) // is cache enabled
            {
                var worldSnapshot = SearchSnapshot(time);
                foreach (var kvp in worldSnapshot.spawns)
                {
                    var reader = ReplayEventFactory.GetEvent<SpawnEvent>();
                    reader.CopyFromSnapshot(kvp.Value);
                    spawns[kvp.Key] = reader;
                }
                moves = worldSnapshot.positions;
                _fileStream.Seek(worldSnapshot.offsetAfter, SeekOrigin.Begin);
                Plugin.SafeLogWarning($"Found world snapshot with time: {worldSnapshot.time}, target time: {time}");
            }
            else
            {
                _fileStream.Seek(_dataStartOffset, SeekOrigin.Begin);
                Plugin.SafeLogWarning($"No snapshot found, reading from start");
            }

            _binaryReader = new BinaryReader(_fileStream); // to be sure that its buffer will be empty after stream seek
            while (_fileStream.Position < producerStartPos)
            {
                var eventType = (EventType)_binaryReader.ReadByte();
                var e = ReplayEventFactory.CreateAndRead(eventType, _binaryReader);
                if (e is SpawnEvent se)
                {
                    spawns[se.unitId] = se;
                }
                else if (e is DespawnEvent de)
                {
                    var spawn = spawns[de.unitId];
                    spawns.Remove(de.unitId);
                    moves.Remove(de.unitId);
                    ReplayEventFactory.Return(e);
                    ReplayEventFactory.Return(spawn);
                }
                else if (e is UpdatePositionEvent upe)
                {
                    var snapshot = new PositionSnapshot
                    {
                        position = upe.position,
                        rotation = upe.rotation,
                        velocity = upe.velocity,
                        time = upe.Time
                    };
                    
                    moves[upe.unitId] = snapshot;

                    ReplayEventFactory.Return(e);
                }
                
                else ReplayEventFactory.Return(e);
            }

            foreach (var kvp in spawns)
            {
                if (moves.ContainsKey(kvp.Key))
                {
                    var move = moves[kvp.Key];
                    kvp.Value.pos = move.position;
                    kvp.Value.rotation = move.rotation;
                    kvp.Value.startingVelocity = move.velocity;
                }
                _restoreBuffer.Enqueue(kvp.Value);
            }
            awaitFrames = 10;
        }
    
        private WorldSnapshot SearchSnapshot(double time)
        {
            int idx = SearchNearestLessIndex(_worldSnapshotTimes, time);
            if(idx < 0) return _worldSnapshots[0];
            return _worldSnapshots[idx];
        }
        private bool _pause = false;
        private Queue<IReplayEvent> _restoreBuffer = new();
        private const int MAX_RESTORE_ACTIONS = 15;
        private int awaitFrames = 0;
        private List<PositionSnapshot> _cameraWaypoints = new();
        private bool _cameraFlightEnabled = false;
        private void Update()
        { 
            if (awaitFrames > 0)
            {
                awaitFrames--;
                return;
            }
            int iterations = 0;
            if (_restoreBuffer.Count > 0)
            {
                while (iterations++ < MAX_RESTORE_ACTIONS && _restoreBuffer.TryDequeue(out var e))
                {
                    e.Execute(_unitCotroller);
                    ReplayEventFactory.Return(e);
                }
                Plugin.logger.LogInfo($"{iterations} restore actions done on this frame");
                Plugin.logger.LogInfo($"To restore: {_restoreBuffer.Count}. Events after: {_buffer.Count}");

                if (_restoreBuffer.Count > 0) return;
            }

            while (_buffer.TryTakeIfReady(currentVirtualTime, out var reader))
            {
                if (reader.EventType != EventType.Move && reader.EventType != EventType.UpdateTurret)
                    Plugin.DebugLog($"Reading event: {reader.EventType}");

                //todo: move this block to unit controller + switch to linked list
                if (reader is UpdatePositionEvent move)
                {
                    var id = move.unitId;
                    var snapshot = new PositionSnapshot
                    {
                        position = move.position,
                        rotation = move.rotation,
                        velocity = move.velocity,
                        time = move.Time
                    };
                    if (_unitSnapshots.TryGetValue(id, out var collection))
                    {
                        collection.Add(snapshot);
                    }
                    else
                    {
                        _unitSnapshots.TryAdd(id, new List<PositionSnapshot>() { snapshot });
                    }
                    ReplayEventFactory.Return(move);
                }
                else
                {
                    reader.Execute(_unitCotroller);
                    ReplayEventFactory.Return(reader);
                }
            }

            foreach (var kvp in _unitSnapshots)
            {
                _unitCotroller.MoveUnit(kvp.Key, kvp.Value, currentVirtualTime); ;
            }

            _unitCotroller.Update(currentVirtualTime);
            if (currentVirtualTime >= replayDuration - 0.01f) ForcePause();

            if (_cameraFlightEnabled)
            {
                if (_cameraWaypoints.Last().time < currentVirtualTime)
                {
                    _cameraWaypoints.Clear();
                    _cameraFlightEnabled = false;
                    ReplayManager.i.StopCamFlight();
                }
                else
                    MoveCamera();
            }
           
            currentVirtualTime += Time.deltaTime;
        }

        private void ForcePause()
        {
            if (!_pause)
            {
                _pause = true;
                TimeScaleManager.Scale = 0;
            }
        }
        public void TogglePause()
        {
            _pause = !_pause;
            if (_pause)
            {
                TimeScaleManager.Scale = 0;
            }
            else
            {
                TimeScaleManager.Scale = 1;
            }
        }
        public bool ShouldContinue(uint id)
        {
            if (_unitCotroller == null) return true;
            return _unitCotroller.ShouldContinuePatchedMethod(id);
        }

        private void MoveCamera()
        {
            var idx = Tools.FindLastIndex(_cameraWaypoints, currentVirtualTime);
            if (idx < 0)
            {
                SetCameraTransform(_cameraWaypoints[0]);
                return;
            }
            if (idx >= _cameraWaypoints.Count - 1)
            {
                SetCameraTransform(_cameraWaypoints[_cameraWaypoints.Count - 1]);
                return;
            }

            var prev = _cameraWaypoints[idx];
            var next = _cameraWaypoints[idx + 1];
            float t = (float)((currentVirtualTime - prev.time) / (next.time - prev.time));
            t = Mathf.Clamp01(t);

            Vector3 pos = Vector3.Lerp(prev.position.ToLocalPosition(), next.position.ToLocalPosition(), t);
            Quaternion rot = Quaternion.Slerp(prev.rotation, next.rotation, t);

            SetCameraTransform(pos, rot);
        }

        private void SetCameraTransform(PositionSnapshot snapshot)
        {
            SetCameraTransform(snapshot.position.ToLocalPosition(), snapshot.rotation);
        }

        private void SetCameraTransform(Vector3 position,  Quaternion rotation)
        {
            CameraStateManager.i.SetCameraPosition(new NuclearOption.SavedMission.PositionRotation()
            {
                Position = position.ToGlobalPosition(),
                Rotation = rotation
            });
        }

        private class BoundedEventBuffer
        {
            //separated buffers
            private ConcurrentQueue<IReplayEvent> _queue = new();
            private ConcurrentQueue<IReplayEvent> _interpolationBuffer = new();
            
            private int _capacity;
            private SemaphoreSlim _freeSpace;
            public int Count { get { return _queue.Count; } }
            public int FreeSpace { get { return _capacity-Count; } }
            public BoundedEventBuffer(int capacity)
            {
                _capacity = capacity;
                _freeSpace = new SemaphoreSlim(capacity, capacity);
            }

            /// <summary>
            /// Adds element to buffer.
            /// Blocks the execution thread if there's no free space in the buffer.
            /// Unblocks as soon as space becomes available.
            /// </summary>
            /// <param name="e"></param>
            /// <param name="token"></param>
            public void Add(IReplayEvent e, CancellationToken token)
            {
                _freeSpace.Wait(token);
                if (e.EventType == EventType.Move ||
                        e.EventType == EventType.UpdateTurret ||
                        e.EventType == EventType.ControlInputs)
                    _interpolationBuffer.Enqueue(e);
                else
                    _queue.Enqueue(e);
            }
            /// <summary>
            /// If an element is extracted from the buffer, returns true, gives IReplayEvent, and frees up space in the buffer.
            /// False if there is no elements in buffer or time check failed.
            /// </summary>
            /// <param name="time"></param>
            /// <param name="e"></param>
            /// <returns></returns>
            public bool TryTakeIfReady(double time, out IReplayEvent e)
            {
                e = null;
                if(_interpolationBuffer.TryPeek(out var check))
                {
                    if(check.EventType == EventType.Move ||
                        check.EventType == EventType.UpdateTurret
                        && time >= check.Time - 4)
                    {
                        _interpolationBuffer.TryDequeue(out e);
                        _freeSpace.Release();
                        return true;
                    }
                }
                if (_queue.TryPeek(out var timeCheck))
                {
                    if (time >= timeCheck.Time)
                    {
                        _queue.TryDequeue(out e);
                        _freeSpace.Release();
                        return true;
                    }
                }
                return false;
            }
            /// <summary>
            /// Returns all events to event bool and frees buffer space.
            /// </summary>
            public void ClearAndReturnToPool()
            {
                while(_queue.TryDequeue(out var e))
                {
                    ReplayEventFactory.Return(e);
                    _freeSpace.Release();
                }
            }
        }
    }
       
}
