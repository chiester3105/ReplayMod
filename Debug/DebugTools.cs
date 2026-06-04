using UnityEngine;

namespace ReplayMod.Debug
{
    public static class DebugTools
    {

        private static Unit currentSelection;

        static DebugTools()
        {
            DynamicMap.i.onUnitSelected += (u) => currentSelection = u;
            DynamicMap.i.onAllDeselected += () => currentSelection = null;
        }
        public static void DisableRbToSelectedAircraft()
        {
            if (!(currentSelection is Aircraft a)) return;
            a.SetSimplePhysics();
            foreach (var part in a.partLookup)
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
            }
        }

        public static void DetachRandomPart()
        {
            if(currentSelection == null)
            {
                currentSelection = CameraStateManager.i.followingUnit;
            }
            if (!(currentSelection is Aircraft a)) return;

            var part = a.GetRandomPart();
            if (part.TryGetComponent<AeroPart>(out var component))
            {
                EnableRb(component);
                component.CreateRB(default, component.transform.position);
                a.DetachPart(component.id, default, default);
            }
        }

        public static void ShowPartLookup()
        {
            if(GameManager.GetLocalAircraft(out var local))
            {
                var parts = local.partLookup;
                foreach(var part in parts)
                {
                    Plugin.logger.LogInfo($"Part id: {part.id}, part name: {part.name}");
                }
            }
        }
        
        private static void EnableRb(UnitPart part)
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
        
        public static void ShowWeaponStations()
        {
            if (currentSelection == null) return;
            var target = currentSelection;
            Plugin.logger.LogInfo($"Unit: {target}");
            foreach(var station in target.weaponStations)
            {
                Plugin.logger.LogInfo($"number: {station.Number},  weaponName {station.WeaponInfo.weaponName}");
            }
        }
    }
}
