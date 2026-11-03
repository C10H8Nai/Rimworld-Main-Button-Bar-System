using HarmonyLib;
using UnityEngine;
using Verse;

namespace MainButtonBarSystem
{
    public class MainButtonBarSystemMod : Mod
    {
        public const string HarmonyId = "NAI.MainButtonBarSystem";
        public const string LogPrefix = "[Main Button Bar System] ";

        public static MainButtonBarSystemMod Instance { get; private set; }
        public static MainButtonBarSystemSettings Settings { get; private set; }

        public MainButtonBarSystemMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<MainButtonBarSystemSettings>();
            new Harmony(HarmonyId).PatchAll(typeof(MainButtonBarSystemMod).Assembly);
            Log.Message(LogPrefix + "Loaded.");
        }

        public override string SettingsCategory() => "Main Button Bar System";

        // The game's mod settings window is small, so it only opens our own window (spec 3.5, 5).
        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label(BarConfigManager.InGame ? "MBBS_ModSettings_InGame".Translate() : "MBBS_ModSettings_MainMenu".Translate());
            if (listing.ButtonText("MBBS_OpenSettings".Translate()))
                Find.WindowStack.Add(new Dialog_MainButtonBar());

            listing.GapLine();
            listing.CheckboxLabeled("MBBS_AlwaysDrawBar".Translate(), ref Settings.alwaysDrawBar, "MBBS_AlwaysDrawBarTip".Translate());

            listing.End();
        }
    }
}
