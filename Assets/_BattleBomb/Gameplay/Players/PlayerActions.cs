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
        public const string Attack = "Attack";
        public const string Heavy = "Heavy";
        public const string Dodge = "Dodge";
        public const string Ability1 = "Ability1";
        public const string Ability2 = "Ability2";
        public const string Interact = "Interact";
    }
}
