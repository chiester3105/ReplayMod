using System;
using System.Reflection;

namespace ReplayMod
{
    public static class ReflectionCache
    {
        public static MethodInfo baseUnitDisabled;
        public static Action<Unit, bool, bool> baseUnitDisabledDelegate;

        public static MethodInfo baseUnitOnDestroy;
        public static Action<Unit> baseUnitOnDestroyDelegate;
        static ReflectionCache()
        {
            

        }

        public static void Init()
        {
            baseUnitDisabled = typeof(Unit).GetMethod("UnitDisabled",
           BindingFlags.Public | BindingFlags.Instance);
            baseUnitDisabledDelegate = (Action<Unit, bool, bool>)Delegate.CreateDelegate(
            typeof(Action<Unit, bool, bool>), null, baseUnitDisabled);

            baseUnitOnDestroy = typeof(Unit).GetMethod("OnDestroy",
                BindingFlags.NonPublic | BindingFlags.Instance);
            baseUnitOnDestroyDelegate = (Action<Unit>)Delegate.CreateDelegate(typeof(Action<Unit>),
                null, baseUnitOnDestroy);
        }
    }
}
