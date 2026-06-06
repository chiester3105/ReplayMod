using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using NuclearOption.SavedMission.ObjectiveV2;
using NuclearOption.SceneLoading;

namespace ReplayMod
{
    public static class SceneHelper
    {
        public static Mission CreateEmptyMission(MapKey mapKey, string missionName)
        {
            Mission mission = MissionSaveLoad.LoadDefault();
            mission.Name = missionName;
            mission.MapKey = mapKey;
           
            mission.aircraft.Clear();
            mission.vehicles.Clear();
            mission.ships.Clear();
            mission.buildings.Clear();
            mission.scenery.Clear();
            mission.containers.Clear();
            mission.missiles.Clear();
            mission.pilots.Clear();

            mission.objectives = new SavedMissionObjectives();
            SavedObjective start = new SavedObjective();
            start.UniqueName = MissionObjectivesFactory.MissionStartName;
            start.Type = ObjectiveType.None;
            start.Hidden = true;
            start.Data = new List<ObjectiveData>();
            mission.objectives.Objectives = new List<SavedObjective> { start };
            mission.objectives.Outcomes = new List<SavedOutcome>();

            return mission;
        }


        public static async UniTask<bool> LoadSceneAsync(MapKey key, string missionName = "__ReplayEmpty")
        {

            await UniTask.SwitchToMainThread();
            Mission m = CreateEmptyMission(key, missionName);
          
            MissionManager.SetMission(m, false);
            
            await NetworkManagerNuclearOption.i.StartHostAsync(new HostOptions(SocketType.Offline, GameState.SinglePlayer, key));
           
            Plugin.logger.LogInfo("Awaitnig mission is running");
            await UniTask.WaitWhile(() => !MissionManager.IsRunning);

            await UniTask.WaitWhile(() => NetworkSceneSingleton<Spawner>.i == null);
            await UniTask.WaitWhile(() => SceneSingleton<MapSettingsManager>.i == null);
            await UniTask.WaitWhile(() => NetworkSceneSingleton<LevelInfo>.i == null);

            await UniTask.Yield();
            
            return true;
        }

        public static async UniTask RestartMisison()
        {
            await MissionManager.RestartMission();
        }

        public static async UniTask<bool> LoadMapOnly(MapKey mapKey)
        {
            await UniTask.WaitUntil(() => NetworkManagerNuclearOption.i != null);
   
            MapLoader mapLoader = NetworkManagerNuclearOption.i.mapLoader;
 
            MapLoader.LoadResult result = await mapLoader.Load(mapKey, null, null);

            if (result == MapLoader.LoadResult.ChangedScene ||
                result == MapLoader.LoadResult.ChangedWorldPrefab ||
                result == MapLoader.LoadResult.AlreadyLoaded)
            {     
                await UniTask.WaitUntil(() => Spawner.i != null && LevelInfo.i != null);
                return true;
            }

            return false;
        }
    }
}
