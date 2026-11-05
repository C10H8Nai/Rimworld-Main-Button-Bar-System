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

        /// <summary>
        /// Optional translation key of a button next to the text that turns the other mod's switch off and saves its
        /// settings (<c>{0}</c> is the other mod's name). Shown only while the switch reads as on.
        /// </summary>
        public string turnOffKey;

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
            try
            {
                if (!ResolveOwner(out object owner, out Type staticOwner, out string member, out problem)) return null;
                object value = staticOwner != null ? StaticMember(staticOwner, member) : OtherModSettings.Get(owner, member);
                if (value == null)
                {
                    problem = "member " + member + " not found or null";
                    return null;
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

        /// <summary>
        /// Turns the other mod's switch off and saves its mod settings, for a notice's <see cref="CompatNotice.turnOffKey"/>
        /// button. False (with the reason) when the switch could not be written; true but with a problem when it was
        /// written for this session and its settings could not be saved.
        /// </summary>
        public bool TryTurnOff(out string problem)
        {
            try
            {
                if (!Detected || settingMember.NullOrEmpty())
                {
                    problem = "not active or no switch declared";
                    return false;
                }
                if (!ResolveOwner(out object owner, out Type staticOwner, out string member, out problem)) return false;
                bool written = staticOwner != null ? SetStaticMember(staticOwner, member, false) : OtherModSettings.Set(owner, member, false);
                if (!written)
                {
                    problem = "member " + member + " cannot be written";
                    return false;
                }
                // The switch is off from here on; a save that fails only means it comes back after a restart.
                var mod = OtherModSettings.ModOf(packageIds);
                if (mod == null)
                {
                    problem = "its mod was not found, so the change lasts only until the game restarts";
                    return true;
                }
                try
                {
                    mod.WriteSettings();
                }
                catch (Exception e)
                {
                    problem = "saving its settings failed (" + e.GetType().Name + ": " + e.Message + "), so the change lasts only until the game restarts";
                }
                return true;
            }
            catch (Exception e)
            {
                problem = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        /// <summary>
        /// Follows <see cref="settingMember"/> up to its last part. That part is a member of <paramref name="owner"/>,
        /// or a static member of <paramref name="staticOwner"/> when the path has only one part after <see cref="settingType"/>.
        /// </summary>
        private bool ResolveOwner(out object owner, out Type staticOwner, out string member, out string problem)
        {
            owner = null;
            staticOwner = null;
            problem = null;
            var parts = settingMember.Split('.');
            member = parts[parts.Length - 1];
            int start;
            if (settingType.NullOrEmpty())
            {
                owner = OtherModSettings.Of(packageIds);
                start = 0;
                if (owner == null)
                {
                    problem = "its mod settings were not found";
                    return false;
                }
            }
            else
            {
                if (!typeResolved)
                {
                    typeResolved = true;
                    resolvedType = GenTypes.GetTypeInAnyAssembly(settingType);
                }
                if (resolvedType == null)
                {
                    problem = "type not found";
                    return false;
                }
                if (parts.Length == 1)
                {
                    staticOwner = resolvedType;
                    return true;
                }
                owner = StaticMember(resolvedType, parts[0]);
                start = 1;
                if (owner == null)
                {
                    problem = "static member " + parts[0] + " not found or null";
                    return false;
                }
            }

            for (int i = start; i < parts.Length - 1; i++)
            {
                owner = OtherModSettings.Get(owner, parts[i]);
                if (owner == null)
                {
                    problem = "member " + parts[i] + " not found or null";
                    return false;
                }
            }
            return true;
        }

        private static object StaticMember(Type type, string name)
        {
            var field = AccessTools.Field(type, name);
            if (field != null) return field.IsStatic ? field.GetValue(null) : null;
            var property = AccessTools.Property(type, name);
            var getter = property?.GetGetMethod(true);
            return getter != null && getter.IsStatic && property.GetIndexParameters().Length == 0 ? property.GetValue(null) : null;
        }

        private static bool SetStaticMember(Type type, string name, object value)
        {
            var field = AccessTools.Field(type, name);
            if (field != null)
            {
                if (!field.IsStatic || field.IsInitOnly) return false;
                field.SetValue(null, value);
                return true;
            }
            var setter = AccessTools.Property(type, name)?.GetSetMethod(true);
            if (setter == null || !setter.IsStatic || setter.GetParameters().Length != 1) return false;
            setter.Invoke(null, new[] { value });
            return true;
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
                if (settingMember.NullOrEmpty() && notices.Any(n => n != null && !n.turnOffKey.NullOrEmpty()))
                    yield return "notices with turnOffKey need settingMember";
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

        /// <summary>The notice to show for this def now, or null; <paramref name="settingOn"/> is whether its switch reads as on.</summary>
        public static CompatNotice NoticeFor(CompatNoticeDef def, out bool settingOn)
        {
            var setting = def.ReadSetting();
            settingOn = setting == true;
            return def.CurrentNotice(setting, CurrentOwner);
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
