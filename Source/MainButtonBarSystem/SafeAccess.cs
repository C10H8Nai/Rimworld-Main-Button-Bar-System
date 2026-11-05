using System;
using HarmonyLib;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Reflection on the game's private fields. A game update can rename a field; then the accessor is null (with one
    /// warning) and only the feature using it turns off, instead of the whole class failing to initialise and every
    /// patch that touches it throwing on each GUI event.
    /// </summary>
    internal static class SafeAccess
    {
        public static AccessTools.FieldRef<T, F> FieldRef<T, F>(string name) where T : class
        {
            try
            {
                return AccessTools.FieldRefAccess<T, F>(name);
            }
            catch (Exception e)
            {
                Log.Warning(MainButtonBarSystemMod.LogPrefix + "Field " + typeof(T).Name + "." + name + " was not found; the feature using it is off. " + e.Message);
                return null;
            }
        }
    }
}
