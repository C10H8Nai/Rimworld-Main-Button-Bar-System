using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Settings shared by every save. The bar arrangement itself lives in each save (BarConfig) and in the
    /// template file, not here.
    /// </summary>
    public class MainButtonBarSystemSettings : ModSettings
    {
        /// <summary>
        /// Draw the bar with this mod even when nothing is customised. For comparing against the game's own
        /// drawing while testing; off by default so an untouched install changes nothing.
        /// </summary>
        public bool alwaysDrawBar;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref alwaysDrawBar, "alwaysDrawBar");
        }
    }
}
