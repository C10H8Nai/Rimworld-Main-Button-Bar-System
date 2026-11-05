using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Reads another mod's ModSettings by field name, for compatibility bridges. Nothing here fails: a missing mod,
    /// field or unexpected type just yields null, so a changed version of the other mod only loses the reference
    /// values, never breaks this mod.
    /// </summary>
    public static class OtherModSettings
    {
        /// <summary>The settings object of the first active mod with one of these package ids, or null.</summary>
        public static ModSettings Of(IEnumerable<string> packageIds)
        {
            try
            {
                var ids = new HashSet<string>(packageIds.Select(p => p.ToLowerInvariant()));
                foreach (var mod in LoadedModManager.ModHandles)
                {
                    var id = mod?.Content?.PackageId;
                    if (id == null || !ids.Contains(id.ToLowerInvariant())) continue;
                    if (AccessTools.Field(typeof(Mod), "modSettings")?.GetValue(mod) is ModSettings settings) return settings;
                }
            }
            catch (Exception e)
            {
                Log.WarningOnce(MainButtonBarSystemMod.LogPrefix + "Could not read another mod's settings: " + e.Message, 0x4D424255);
            }
            return null;
        }

        /// <summary>
        /// The Mod object of the first active mod with one of these package ids, or null, to save its settings with
        /// <see cref="Mod.WriteSettings"/>. Ignores the "_steam" suffix the game adds when a local and a Steam copy of
        /// the mod both exist.
        /// </summary>
        public static Mod ModOf(IEnumerable<string> packageIds)
        {
            var ids = new HashSet<string>(packageIds.Select(p => p.ToLowerInvariant()));
            foreach (var mod in LoadedModManager.ModHandles)
            {
                var id = mod?.Content?.PackageId?.ToLowerInvariant();
                if (id == null) continue;
                if (id.EndsWith(SteamSuffix)) id = id.Substring(0, id.Length - SteamSuffix.Length);
                if (ids.Contains(id)) return mod;
            }
            return null;
        }

        private const string SteamSuffix = "_steam";

        /// <summary>
        /// Sets a field or a settable parameterless property; false when there is none or it cannot be set, and for a
        /// struct (the write would only change a boxed copy).
        /// </summary>
        public static bool Set(object obj, string name, object value)
        {
            if (obj == null || obj.GetType().IsValueType) return false;
            try
            {
                var field = AccessTools.Field(obj.GetType(), name);
                if (field != null && !field.IsStatic && !field.IsInitOnly)
                {
                    field.SetValue(obj, value);
                    return true;
                }
                var setter = AccessTools.Property(obj.GetType(), name)?.GetSetMethod(true);
                if (setter == null || setter.IsStatic || setter.GetParameters().Length != 1) return false;
                setter.Invoke(obj, new[] { value });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>The root folder of the first active mod with one of these package ids, or null.</summary>
        public static string RootDirOf(IEnumerable<string> packageIds)
        {
            var ids = new HashSet<string>(packageIds.Select(p => p.ToLowerInvariant()));
            return LoadedModManager.RunningModsListForReading.FirstOrDefault(m => m.PackageId != null && ids.Contains(m.PackageId.ToLowerInvariant()))?.RootDir;
        }

        public static object Get(object obj, string name)
        {
            if (obj == null) return null;
            try
            {
                var field = AccessTools.Field(obj.GetType(), name);
                if (field != null) return field.GetValue(obj);
                var property = AccessTools.Property(obj.GetType(), name);
                return property?.GetIndexParameters().Length == 0 ? property.GetValue(obj) : null;
            }
            catch
            {
                return null;
            }
        }

        public static string String(object obj, string name) => Get(obj, name) as string;

        public static bool? Bool(object obj, string name) => Get(obj, name) is bool b ? b : (bool?)null;

        public static float? Number(object obj, string name) => Get(obj, name) switch
        {
            float f => f,
            int i => i,
            double d => (float)d,
            _ => null,
        };

        /// <summary>Items of a list or other sequence field; empty when it is not one.</summary>
        public static List<object> Sequence(object obj, string name)
        {
            var value = Get(obj, name);
            if (value is string || !(value is IEnumerable e)) return new List<object>();
            return e.Cast<object>().ToList();
        }

        /// <summary>Entries of a dictionary field as (defName, value text); empty when it is not one.</summary>
        public static List<KeyValuePair<string, string>> Map(object obj, string name)
        {
            var result = new List<KeyValuePair<string, string>>();
            if (!(Get(obj, name) is IDictionary dict)) return result;
            foreach (DictionaryEntry entry in dict)
            {
                var key = DefNameOf(entry.Key);
                if (key != null) result.Add(new KeyValuePair<string, string>(key, entry.Value?.ToString() ?? ""));
            }
            return result;
        }

        /// <summary>A button reference stored as a def, a defName string, or anything whose text is the defName.</summary>
        public static string DefNameOf(object item) => item switch
        {
            null => null,
            Def def => def.defName,
            string s => s,
            _ => item.ToString(),
        };

        public static List<string> DefNames(object obj, string name) =>
            Sequence(obj, name).Select(DefNameOf).Where(n => !string.IsNullOrEmpty(n)).ToList();
    }
}
