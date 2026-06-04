using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReplayMod.Events;

namespace ReplayMod.Data
{
    public struct WorldSnapshot
    {
        public double time;
        public Dictionary<uint, SpawnSnapshot> spawns;
        public Dictionary<uint, PositionSnapshot> positions;
        public long offsetAfter; // file pos after all events in snapshot

        public static WorldSnapshot Create(double time, Dictionary<uint, SpawnSnapshot> spawns,
            Dictionary<uint, PositionSnapshot> positions, long fileOffset)
        {
            Dictionary<uint, SpawnSnapshot> ss = new();
            foreach (var kvp in spawns)
            {
                ss.Add(kvp.Key, kvp.Value);
            }
            Dictionary<uint, PositionSnapshot> ps = new();
            foreach (var kvp in positions)
            {
                ps.Add(kvp.Key, kvp.Value);
            }
            return new WorldSnapshot
            {
                time = time,
                spawns = ss,
                positions = ps,
                offsetAfter = fileOffset
            };
        }
    }
}
