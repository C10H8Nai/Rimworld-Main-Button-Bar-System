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

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("MBBS_Test_Header".Translate());
            listing.GapLine();

            if (listing.RadioButton("MBBS_Test_GameDraws".Translate(), Settings.testRenderMode == TestRenderMode.GameDraws))
                Settings.testRenderMode = TestRenderMode.GameDraws;
            if (listing.RadioButton("MBBS_Test_OwnVanilla".Translate(), Settings.testRenderMode == TestRenderMode.OwnVanillaLayout))
                Settings.testRenderMode = TestRenderMode.OwnVanillaLayout;
            if (listing.RadioButton("MBBS_Test_OwnScroll".Translate(), Settings.testRenderMode == TestRenderMode.OwnScroll))
                Settings.testRenderMode = TestRenderMode.OwnScroll;

            listing.Gap();
            listing.Label("MBBS_Test_DefaultWidth".Translate(Settings.scrollDefaultWidth));
            Settings.scrollDefaultWidth = (int)listing.Slider(Settings.scrollDefaultWidth, 40f, 300f);
            listing.Label("MBBS_Test_MinWidth".Translate(Settings.scrollMinWidth));
            Settings.scrollMinWidth = (int)listing.Slider(Settings.scrollMinWidth, 20f, 300f);
            listing.CheckboxLabeled("MBBS_Test_FillScreen".Translate(), ref Settings.fillScreen);
            listing.CheckboxLabeled("MBBS_Test_PinMenu".Translate(), ref Settings.pinMenuRight);

            listing.Gap();
            listing.Label("MBBS_Test_Note".Translate());

            listing.End();
        }
    }
}
