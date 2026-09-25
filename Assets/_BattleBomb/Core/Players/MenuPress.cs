namespace BattleBomb.Core.Players
{
    /// <summary>
    /// What a screen should do with one step's command (D57). Every menu reads its buttons
    /// through this, so the one rule that is easy to get wrong lives in one place: Escape is
    /// bound to both Back and Pause, and a step that carries Back is a Back. Start — Pause on
    /// its own — is the pad's way to leave a screen outright.
    /// </summary>
    public readonly struct MenuPress
    {
        public readonly bool Confirm;
        public readonly bool Back;
        public readonly bool Pause;
        public readonly bool Option;
        public readonly bool Lock;

        /// <summary>-1 for the left shoulder, +1 for the right, 0 for neither or both.</summary>
        public readonly int Tab;

        private MenuPress(bool confirm, bool back, bool pause, bool option, bool locking, int tab)
        {
            Confirm = confirm;
            Back = back;
            Pause = pause;
            Option = option;
            Lock = locking;
            Tab = tab;
        }

        public bool Any => Confirm || Back || Pause || Option || Lock || Tab != 0;

        public static MenuPress From(in PlayerCommand command)
        {
            bool back = command.WasPressed(CommandButtons.Back);
            int tab = (command.WasPressed(CommandButtons.TabNext) ? 1 : 0)
                - (command.WasPressed(CommandButtons.TabPrevious) ? 1 : 0);

            return new MenuPress(
                command.WasPressed(CommandButtons.Confirm),
                back,
                command.WasPressed(CommandButtons.Pause) && !back,
                command.WasPressed(CommandButtons.Option),
                command.WasPressed(CommandButtons.Lock),
                tab);
        }

        /// <summary>
        /// Any button at all held. A screen that has just opened ignores input until this is false
        /// once: most menu buttons share a physical button with a fight button, so the press that
        /// opened the screen — X, which is Light and Option — would otherwise act inside it.
        /// </summary>
        public static bool AnyHeld(in PlayerCommand command) => command.Held != CommandButtons.None;
    }
}
