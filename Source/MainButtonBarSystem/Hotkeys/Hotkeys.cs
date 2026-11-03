using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Hotkeys for main buttons (spec 3.6). Buttons that have a KeyBindingDef use the game's KeyPrefs, so the
    /// key is the same as in Options > Keyboard. Buttons without one get a key stored in this mod's settings.
    /// Hidden and drawer buttons keep their hotkeys because the game's button list is never modified.
    /// </summary>
    public static class Hotkeys
    {
        private static Dictionary<string, KeyCode> Custom => MainButtonBarSystemMod.Settings.customHotkeys;

        public static bool UsesGameBinding(MainButtonDef def) => def?.hotKey != null;

        public static KeyCode Get(MainButtonDef def)
        {
            if (def == null) return KeyCode.None;
            if (UsesGameBinding(def))
            {
                try
                {
                    return KeyPrefs.KeyPrefsData.GetBoundKeyCode(def.hotKey, KeyPrefs.BindingSlot.A);
                }
                catch
                {
                    return KeyCode.None;
                }
            }
            return Custom.TryGetValue(def.defName, out var key) ? key : KeyCode.None;
        }

        public static string Label(MainButtonDef def)
        {
            var key = Get(def);
            return key == KeyCode.None ? "—" : key.ToStringReadable();
        }

        /// <summary>Opens a "press a key" dialog for the button.</summary>
        public static void Define(MainButtonDef def)
        {
            if (UsesGameBinding(def))
                Find.WindowStack.Add(new Dialog_DefineBindingAndSave(KeyPrefs.KeyPrefsData, def.hotKey, KeyPrefs.BindingSlot.A));
            else
                Find.WindowStack.Add(new Dialog_CaptureKey(key => SetCustom(def, key)));
        }

        public static void Clear(MainButtonDef def)
        {
            if (UsesGameBinding(def))
            {
                KeyPrefs.KeyPrefsData.SetBinding(def.hotKey, KeyPrefs.BindingSlot.A, KeyCode.None);
                KeyPrefs.Save();
            }
            else
            {
                SetCustom(def, KeyCode.None);
            }
        }

        private static void SetCustom(MainButtonDef def, KeyCode key)
        {
            if (key == KeyCode.None) Custom.Remove(def.defName);
            else Custom[def.defName] = key;
            MainButtonBarSystemMod.Instance.WriteSettings();
        }

        /// <summary>One binding that shares the button's key, described for the player.</summary>
        public readonly struct Conflict
        {
            /// <summary>Where the other binding is listed, e.g. a key binding category or "Main buttons".</summary>
            public readonly string Category;
            public readonly string Label;

            public Conflict(string category, string label)
            {
                Category = category;
                Label = label;
            }
        }

        /// <summary>
        /// Other bindings that really conflict, by the game's own rule (KeyPrefsData.ConflictingBindings): the other
        /// binding counts only if it is in the same self-conflicting category, in a category this one's
        /// <c>checkForConflicts</c> lists, or shares an <c>extraConflictTags</c> entry; <c>ignoreConflictsWith</c> is
        /// skipped. Keys this mod stores itself are treated as the MainTabs category.
        /// </summary>
        public static List<Conflict> Conflicts(MainButtonDef def)
        {
            var result = new List<Conflict>();
            var key = Get(def);
            if (key == KeyCode.None) return result;

            bool custom = !UsesGameBinding(def);
            var mainTabs = KeyBindingCategoryDefOf.MainTabs;
            var ownCategory = def.hotKey?.category ?? mainTabs;
            var data = KeyPrefs.KeyPrefsData;
            if (data?.keyPrefs != null)
            {
                foreach (var pair in data.keyPrefs)
                {
                    var other = pair.Key;
                    if (other == null || other == def.hotKey || pair.Value == null) continue;
                    if (pair.Value.keyBindingA != key && pair.Value.keyBindingB != key) continue;
                    // A key this mod stores is handled after the game's main button keys, so any main button
                    // key on the same key takes it first; for everything else the game's rule applies.
                    bool swallowsCustom = custom && other.category == mainTabs;
                    if (!swallowsCustom && !InConflictScope(def.hotKey, ownCategory, other)) continue;
                    result.Add(new Conflict(other.category?.LabelCap ?? "", other.LabelCap));
                }
            }

            // Keys this mod stores: only the first match fires, so two of them on one key always conflict, and a
            // stored key on the same key as this game-bound button never fires.
            foreach (var pair in Custom)
            {
                if (pair.Key == def.defName || pair.Value != key) continue;
                var label = DefDatabase<MainButtonDef>.GetNamedSilentFail(pair.Key)?.LabelCap ?? pair.Key;
                result.Add(new Conflict("MBBS_HotKeyCustomCategory".Translate(), label));
            }
            return result;
        }

        private static bool InConflictScope(KeyBindingDef own, KeyBindingCategoryDef ownCategory, KeyBindingDef other)
        {
            if (own?.ignoreConflictsWith != null && own.ignoreConflictsWith.Contains(other)) return false;
            if (other.category == ownCategory && ownCategory.selfConflicting) return true;
            if (ownCategory.checkForConflicts != null && other.category != null && ownCategory.checkForConflicts.Contains(other.category)) return true;
            if (own?.extraConflictTags != null && other.extraConflictTags != null)
            {
                foreach (var tag in own.extraConflictTags)
                    if (other.extraConflictTags.Contains(tag)) return true;
            }
            return false;
        }

        /// <summary>
        /// Fires custom hotkeys. Runs in HandleLowPriorityShortcuts, after windows, so a focused text field gets
        /// the key first.
        /// </summary>
        public static void HandleCustomHotkeys()
        {
            var ev = Event.current;
            if (ev == null || ev.type != EventType.KeyDown || ev.keyCode == KeyCode.None || Custom.Count == 0) return;
            // Same gates as the game's KeyBindingDef.KeyDownEvent: not while a search box has focus, and not with
            // Command/Windows held. Focused text fields already used the key, since this runs after windows.
            if (Find.WindowStack.AnySearchWidgetFocused) return;
            if (ev.command && ev.keyCode != KeyCode.LeftCommand && ev.keyCode != KeyCode.RightCommand) return;

            foreach (var pair in Custom)
            {
                if (pair.Value != ev.keyCode) continue;
                var def = DefDatabase<MainButtonDef>.GetNamedSilentFail(pair.Key);
                if (def?.Worker == null || def.Worker.Disabled) continue;
                def.Worker.InterfaceTryActivate();
                ev.Use();
                return;
            }
        }
    }

    /// <summary>The game's key binding dialog, saving KeyPrefs.xml when it closes.</summary>
    public class Dialog_DefineBindingAndSave : Dialog_DefineBinding
    {
        public Dialog_DefineBindingAndSave(KeyPrefsData keyPrefsData, KeyBindingDef keyDef, KeyPrefs.BindingSlot slot)
            : base(keyPrefsData, keyDef, slot)
        {
        }

        public override void PostClose()
        {
            base.PostClose();
            KeyPrefs.Save();
        }
    }

    /// <summary>"Press a key" for buttons that have no KeyBindingDef.</summary>
    public class Dialog_CaptureKey : Window
    {
        private readonly Action<KeyCode> onKey;

        public override Vector2 InitialSize => MainButtonBarTuningDef.Current.settingsWindow.captureKeyDialogSize;

        public Dialog_CaptureKey(Action<KeyCode> onKey)
        {
            this.onKey = onKey;
            closeOnAccept = false;
            closeOnCancel = false;
            forcePause = true;
            absorbInputAroundWindow = true;
            onlyOneOfTypeAllowed = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, "MBBS_PressKey".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            var ev = Event.current;
            if (ev.isKey && ev.type == EventType.KeyDown && ev.keyCode != KeyCode.None)
            {
                if (ev.keyCode != KeyCode.Escape) onKey(ev.keyCode);
                ev.Use();
                Close();
            }
        }
    }
}
