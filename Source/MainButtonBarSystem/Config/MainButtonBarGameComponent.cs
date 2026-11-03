using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Holds this save's bar config (spec 3.4). A new game, or a save that never had this mod, starts from the
    /// template. Removing the mod from a save makes the game log one missing-class message on the next load.
    /// </summary>
    public class MainButtonBarGameComponent : GameComponent
    {
        public BarConfig config;

        public MainButtonBarGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref config, "config");
        }

        public override void FinalizeInit()
        {
            if (config == null) config = BarConfigManager.NewFromTemplate();
        }
    }
}
