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

        /// <summary>Names of other bindings that use the same key, for the conflict warning.</summary>
        public static List<string> Conflicts(MainButtonDef def)
        {
            var result = new List<string>();
            var key = Get(def);
            if (key == KeyCode.None) return result;

            var data = KeyPrefs.KeyPrefsData;
            if (data?.keyPrefs != null)
            {
                foreach (var pair in data.keyPrefs)
                {
                    if (pair.Key == def.hotKey || pair.Value == null) continue;
                    if (pair.Value.keyBindingA == key || pair.Value.keyBindingB == key) result.Add(pair.Key.LabelCap);
                }
            }
            foreach (var pair in Custom)
            {
                if (pair.Key == def.defName || pair.Value != key) continue;
                result.Add(DefDatabase<MainButtonDef>.GetNamedSilentFail(pair.Key)?.LabelCap ?? pair.Key);
            }
            return result;
        }

        /// <summary>
        /// Fires custom hotkeys. Runs in HandleLowPriorityShortcuts, after windows, so a focused text field gets
        /// the key first.
        /// </summary>
        public static void HandleCustomHotkeys()
        {
            var ev = Event.current;
            if (ev == null || ev.type != EventType.KeyDown || ev.keyCode == KeyCode.None || Custom.Count == 0) return;
            if (GUIUtility.keyboardControl != 0) return;

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

        public override Vector2 InitialSize => new Vector2(400f, 160f);

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
