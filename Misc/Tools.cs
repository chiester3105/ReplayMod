using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReplayMod.Data;

namespace ReplayMod.Misc
{
    public static class Tools
    {
        
        public static int FindLastIndex(List<PositionSnapshot> positions, double time)
        {
            int lo = 0, hi = positions.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (positions[mid].time <= time)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return best;
        }
        public static int FindLastIndex(List<TurretSnapshot> positions, double time)
        {
            int lo = 0, hi = positions.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (positions[mid].time <= time)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return best;
        }

        public static int FindLastIndex(List<Inputs> positions, double time)
        {
            int lo = 0, hi = positions.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (positions[mid].time <= time)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return best;
        }
    }
}
