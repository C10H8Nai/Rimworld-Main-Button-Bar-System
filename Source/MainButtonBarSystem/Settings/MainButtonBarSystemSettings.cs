using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Stage 1 test switch: who draws the bar. Replaced by per-save bar settings in a later stage.
    /// </summary>
    public enum TestRenderMode
    {
        /// <summary>Do not take over; the game's own DoButtons draws the bar. Default, so installing changes nothing.</summary>
        GameDraws,

        /// <summary>This mod draws the bar using the vanilla equal-split layout. Should look identical to GameDraws.</summary>
        OwnVanillaLayout,

        /// <summary>This mod draws the bar in scroll mode.</summary>
        OwnScroll,
    }

    public class MainButtonBarSystemSettings : ModSettings
    {
        public TestRenderMode testRenderMode = TestRenderMode.GameDraws;
        public int scrollDefaultWidth = 120;
        public int scrollMinWidth = 100;
        public bool fillScreen = true;
        public bool pinMenuRight = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref testRenderMode, "testRenderMode", TestRenderMode.GameDraws);
            Scribe_Values.Look(ref scrollDefaultWidth, "scrollDefaultWidth", 120);
            Scribe_Values.Look(ref scrollMinWidth, "scrollMinWidth", 100);
            Scribe_Values.Look(ref fillScreen, "fillScreen", true);
            Scribe_Values.Look(ref pinMenuRight, "pinMenuRight", true);
        }
    }
}
