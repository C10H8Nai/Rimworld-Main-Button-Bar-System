using HarmonyLib;
using Verse;

namespace MainButtonBarSystem
{
    public class MainButtonBarSystemMod : Mod
    {
        public const string HarmonyId = "NAI.MainButtonBarSystem";

        public MainButtonBarSystemMod(ModContentPack content) : base(content)
        {
            new Harmony(HarmonyId).PatchAll(typeof(MainButtonBarSystemMod).Assembly);
            Log.Message("[Main Button Bar System] Loaded.");
        }
    }
}
