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
    }
}
