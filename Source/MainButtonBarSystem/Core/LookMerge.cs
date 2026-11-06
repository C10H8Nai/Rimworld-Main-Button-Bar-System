namespace MainButtonBarSystem.Core
{
    /// <summary>Layers of a custom look, most specific first (design spec 2026-11-05 §4.5).</summary>
    public enum LookLayer
    {
        Button,
        AllButtons,
        Theme,
    }

    /// <summary>Button states a per-state value (label colour, background) is looked up for; numbered like ButtonVisualState.</summary>
    public enum LookState
    {
        Normal,
        Mouseover,
        Active,
        Disabled,
    }

    /// <summary>
    /// The three-layer rules of a custom look, with no dependency on the game: this button's value, else all buttons',
    /// else the theme's. Per-state values first try every layer for the state itself, and only then the next state down
    /// (open → mouse-over → normal, disabled → normal). Which layer has a value for which state is passed as a bit mask
    /// (<see cref="Bit"/>), so a lookup on every frame allocates nothing.
    /// </summary>
    public static class LookMerge
    {
        private const int StateCount = 4;

        /// <summary>The mask bit saying <paramref name="layer"/> has a value for <paramref name="state"/>.</summary>
        public static int Bit(LookLayer layer, LookState state) => 1 << ((int)layer * StateCount + (int)state);

        /// <summary>The state tried after <paramref name="state"/> when no layer has a value for it; false after normal.</summary>
        public static bool Next(LookState state, out LookState next)
        {
            switch (state)
            {
                case LookState.Active:
                    next = LookState.Mouseover;
                    return true;
                case LookState.Mouseover:
                case LookState.Disabled:
                    next = LookState.Normal;
                    return true;
                default:
                    next = LookState.Normal;
                    return false;
            }
        }

        /// <summary>
        /// The layer and state whose value a button in <paramref name="state"/> uses, given which layers have values
        /// (<paramref name="mask"/>, built with <see cref="Bit"/>). False when no layer has one for any state on the way.
        /// </summary>
        public static bool Find(LookState state, int mask, out LookLayer layer, out LookState found)
        {
            var s = state;
            while (true)
            {
                for (var l = LookLayer.Button; l <= LookLayer.Theme; l++)
                {
                    if ((mask & Bit(l, s)) == 0) continue;
                    layer = l;
                    found = s;
                    return true;
                }
                if (!Next(s, out s)) break;
            }
            layer = LookLayer.Theme;
            found = LookState.Normal;
            return false;
        }

        /// <summary>A value without states: this button's when set, else all buttons' when set, else <paramref name="fallback"/>.</summary>
        public static T Pick<T>(bool buttonSet, T button, bool allSet, T all, T fallback) => buttonSet ? button : allSet ? all : fallback;
    }
}
