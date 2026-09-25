namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// Action names as authored in <c>Assets/_BattleBomb/Input/BattleBombControls.inputactions</c>.
    /// Referenced by name because <c>PlayerInput</c> clones the asset per player; these constants are
    /// the single place a rename has to be reflected.
    /// </summary>
    public static class PlayerActions
    {
        public const string Map = "Gameplay";

        public const string Move = "Move";
        public const string Light = "Light";
        public const string Heavy = "Heavy";
        public const string Magic = "Magic";
        public const string Equipment = "Equipment";
        public const string Jump = "Jump";

        /// <summary>Opens the settings menu (M6) — a system button, not a combat verb.</summary>
        public const string Pause = "Pause";

        /// <summary>The menu layer's own map (D57): screens read these, the fight never does.</summary>
        public const string MenuMap = "Menu";

        public const string Confirm = "Confirm";
        public const string Back = "Back";
        public const string Option = "Option";
        public const string Lock = "Lock";
        public const string TabPrevious = "TabPrevious";
        public const string TabNext = "TabNext";
    }
}
