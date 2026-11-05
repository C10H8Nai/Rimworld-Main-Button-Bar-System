using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>State of the other mod's switch that a notice asks for.</summary>
    public enum CompatSettingState
    {
        /// <summary>Any state, including when the switch cannot be read.</summary>
        Any,
        On,
        Off,

        /// <summary>The switch could not be read (the other mod changed, or the path is wrong).</summary>
        Unknown,
    }

    /// <summary>Who drew the bar on the last draw, as a notice asks for it.</summary>
    public enum CompatBarOwner
    {
        /// <summary>Anyone, and also at the main menu, where no bar is drawn.</summary>
        Any,
        ThisMod,

        /// <summary>The game or another mod (in a game only): either of the two below.</summary>
        NotThisMod,

        /// <summary>This mod's prefix ran and left the bar to the game or a later mod (its settings are untouched).</summary>
        ThisModYielded,

        /// <summary>An earlier prefix drew the bar, so this mod's prefix did not run (that mod is above this one).</summary>
        ThisModSkipped,
    }

    /// <summary>One notice of a <see cref="CompatNoticeDef"/>; the first one whose conditions hold is shown.</summary>
    public class CompatNotice
    {
        public CompatSettingState setting = CompatSettingState.Any;
        public CompatBarOwner bar = CompatBarOwner.Any;

        /// <summary>Translation key of the text; <c>{0}</c> is the other mod's name.</summary>
        public string textKey;

        public bool Matches(bool? settingValue, CompatBarOwner? owner)
        {
            switch (setting)
            {
                case CompatSettingState.On when settingValue != true:
                case CompatSettingState.Off when settingValue != false:
                case CompatSettingState.Unknown when settingValue.HasValue:
                    return false;
            }
            if (bar == CompatBarOwner.Any || owner == bar) return true;
            return bar == CompatBarOwner.NotThisMod && (owner == CompatBarOwner.ThisModYielded || owner == CompatBarOwner.ThisModSkipped);
        }
    }

    /// <summary>
    /// Slot "compatibility notice": another mod that overlaps with this one but is left alone. It only puts a notice
    /// at the top of the settings window, chosen from the other mod's on/off switch and from who drew the bar; there
    /// is no priority choice and none of its patches are removed (compare <see cref="CompatBridgeDef"/>). Everything
    /// is declared in XML; the switch is read by reflection, and a switch that cannot be read counts as unknown.
    /// </summary>
    public class CompatNoticeDef : Def
    {
        /// <summary>Package ids of the other mod; any active one counts as detected.</summary>
        public List<string> packageIds = new List<string>();

        /// <summary>Name shown to the player.</summary>
        public string modName;

        /// <summary>
        /// Full name of the type whose static member starts <see cref="settingMember"/>. Leave empty to start from the
        /// other mod's ModSettings object instead.
        /// </summary>
        public string settingType;

        /// <summary>Dot-separated path to a bool: a static member of <see cref="settingType"/> first (or a member of the
        /// ModSettings object), then instance members. Fields and parameterless properties both work.</summary>
        public string settingMember;

        public List<CompatNotice> notices = new List<CompatNotice>();

        [Unsaved] private bool? detected;
        [Unsaved] private bool unreadableLogged;
        [Unsaved] private bool typeResolved;
        [Unsaved] private Type resolvedType;

        public string ModNameOrLabel => modName ?? LabelCap;

        // Ignoring the "_steam" suffix the game adds when a local and a Steam copy of the mod both exist.
        public bool Detected => detected ??= ModLister.AnyModActiveNoSuffix(packageIds);

        /// <summary>The other mod's switch, or null when it is not active or the switch cannot be read (logged once in development mode).</summary>
        public bool? ReadSetting()
        {
            if (!Detected || settingMember.NullOrEmpty()) return null;
            var value = TryRead(out string problem);
            if (value == null && !unreadableLogged)
            {
                unreadableLogged = true;
                if (Prefs.DevMode)
                    Log.Message(MainButtonBarSystemMod.LogPrefix + ModNameOrLabel + ": could not read " + SettingPath + " (" + problem + "); treated as unknown.");
            }
            return value;
        }

        /// <summary>The notice to show now, or null.</summary>
        public CompatNotice CurrentNotice(bool? settingValue, CompatBarOwner? owner)
        {
            if (!Detected) return null;
            foreach (var notice in notices)
                if (notice != null && notice.Matches(settingValue, owner)) return notice;
            return null;
        }

        public string SettingPath => settingType.NullOrEmpty() ? "settings." + settingMember : settingType + "." + settingMember;

        private bool? TryRead(out string problem)
        {
            problem = null;
            try
            {
                var parts = settingMember.Split('.');
                object value;
                int start;
                if (settingType.NullOrEmpty())
                {
                    value = OtherModSettings.Of(packageIds);
                    start = 0;
                    if (value == null)
                    {
                        problem = "its mod settings were not found";
                        return null;
                    }
                }
                else
                {
                    if (!typeResolved)
                    {
                        typeResolved = true;
                        resolvedType = GenTypes.GetTypeInAnyAssembly(settingType);
                    }
                    var type = resolvedType;
                    if (type == null)
                    {
                        problem = "type not found";
                        return null;
                    }
                    value = StaticMember(type, parts[0]);
                    start = 1;
                    if (value == null)
                    {
                        problem = "static member " + parts[0] + " not found or null";
                        return null;
                    }
                }

                for (int i = start; i < parts.Length; i++)
                {
                    value = OtherModSettings.Get(value, parts[i]);
                    if (value == null)
                    {
                        problem = "member " + parts[i] + " not found or null";
                        return null;
                    }
                }

                if (value is bool b) return b;
                problem = "it is " + value.GetType().Name + ", not bool";
                return null;
            }
            catch (Exception e)
            {
                problem = e.GetType().Name + ": " + e.Message;
                return null;
            }
        }

        private static object StaticMember(Type type, string name)
        {
            var field = AccessTools.Field(type, name);
            if (field != null) return field.IsStatic ? field.GetValue(null) : null;
            var property = AccessTools.Property(type, name);
            var getter = property?.GetGetMethod(true);
            return getter != null && getter.IsStatic && property.GetIndexParameters().Length == 0 ? property.GetValue(null) : null;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors()) yield return e;
            if (packageIds.NullOrEmpty()) yield return "packageIds is empty";
            if (notices.NullOrEmpty()) yield return "notices is empty";
            else
            {
                if (notices.Any(n => n == null || n.textKey.NullOrEmpty())) yield return "every notice needs a textKey";
                if (settingMember.NullOrEmpty() && notices.Any(n => n != null && (n.setting == CompatSettingState.On || n.setting == CompatSettingState.Off)))
                    yield return "notices that depend on the setting need settingMember";
            }
        }
    }

    /// <summary>Access to the compatibility notices for the rest of the mod.</summary>
    public static class CompatNotices
    {
        public static IEnumerable<CompatNoticeDef> Detected => DefDatabase<CompatNoticeDef>.AllDefs.Where(d => d.Detected);

        private static bool lastPrefixRan;

        /// <summary>Called by the DoButtons postfix: whether this mod's prefix ran in that call.</summary>
        internal static void NoteDraw(bool prefixRan) => lastPrefixRan = prefixRan;

        /// <summary>Who drew the bar on the last draw; null at the main menu, where there is no bar.</summary>
        public static CompatBarOwner? CurrentOwner
        {
            get
            {
                if (!BarConfigManager.InGame) return null;
                if (MainButtonBarAPI.DrawnByThisMod) return CompatBarOwner.ThisMod;
                return lastPrefixRan ? CompatBarOwner.ThisModYielded : CompatBarOwner.ThisModSkipped;
            }
        }

        /// <summary>The text to show for this def now, or null.</summary>
        public static string TextFor(CompatNoticeDef def)
        {
            var notice = def.CurrentNotice(def.ReadSetting(), CurrentOwner);
            return notice == null ? null : notice.textKey.Translate(def.ModNameOrLabel).ToString();
        }

        private static bool logged;

        /// <summary>Development mode: one line per notice def on the first bar draw (detected, switch value, who drew).</summary>
        internal static void LogOnce()
        {
            if (logged) return;
            logged = true;
            if (!Prefs.DevMode) return;
            foreach (var def in DefDatabase<CompatNoticeDef>.AllDefs)
            {
                if (!def.Detected)
                {
                    Log.Message(MainButtonBarSystemMod.LogPrefix + def.ModNameOrLabel + ": not active.");
                    continue;
                }
                var value = def.ReadSetting();
                string owner = CurrentOwner switch
                {
                    CompatBarOwner.ThisMod => "this mod",
                    CompatBarOwner.ThisModYielded => "the game or a later mod (this mod left it: its settings are untouched)",
                    _ => "an earlier mod (this mod's prefix was skipped)",
                };
                Log.Message(MainButtonBarSystemMod.LogPrefix + def.ModNameOrLabel + ": active; " + def.SettingPath + " = " +
                            (value.HasValue ? value.Value.ToString() : "unknown") + "; bar drawn by " + owner + ".");
            }
        }
    }
}
