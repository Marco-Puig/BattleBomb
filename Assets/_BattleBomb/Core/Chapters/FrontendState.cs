using UnityEngine;

namespace BattleBomb.Core.Chapters
{
    public enum FrontendScreen
    {
        Title = 0,
        Characters = 1,
        Chapters = 2,
        Launching = 3,
    }

    /// <summary>
    /// The front door's state (D51): title → characters → chapters. Two couch slots; slot 0 is
    /// always present, slot 1 joins with a press at character select, Castle Crashers style.
    /// The chapter picker's own truth is <see cref="StageSelection"/>; this only knows which
    /// screen is up and who is ready.
    /// </summary>
    public sealed class FrontendState
    {
        public const int Slots = 2;

        private readonly int _rosterCount;
        private readonly bool _canContinue;
        private readonly bool[] _joined = new bool[Slots];
        private readonly bool[] _ready = new bool[Slots];
        private readonly int[] _pick = new int[Slots];

        /// <summary>Both slots start on the first character. Seeding slot 1 elsewhere so the
        /// couch does not open on two of the same face is tempting, but nothing stops the two
        /// picks meeting anyway, so the offset would only mean the cursor is somewhere the
        /// player did not put it.</summary>
        public FrontendState(int rosterCount, bool canContinue)
        {
            _rosterCount = Mathf.Max(1, rosterCount);
            _canContinue = canContinue;
            _joined[0] = true;
        }

        public FrontendScreen Screen { get; private set; } = FrontendScreen.Title;

        /// <summary>Continue (when offered), then Start.</summary>
        public int TitleOptions => _canContinue ? 2 : 1;

        public int TitleCursor { get; private set; }

        public bool ChoseContinue { get; private set; }

        public bool IsJoined(int slot) => slot >= 0 && slot < Slots && _joined[slot];

        public bool IsReady(int slot) => slot >= 0 && slot < Slots && _ready[slot];

        public int PickOf(int slot) => slot >= 0 && slot < Slots ? _pick[slot] : 0;

        public void MoveTitle(int delta) => TitleCursor = Mathf.Clamp(TitleCursor + delta, 0, TitleOptions - 1);

        public void MovePick(int slot, int delta)
        {
            if (Screen != FrontendScreen.Characters || !IsJoined(slot) || IsReady(slot))
            {
                return;
            }

            _pick[slot] = (_pick[slot] + delta + _rosterCount) % _rosterCount;
        }

        /// <summary>Light. On the title, the row under the cursor; at characters, join or
        /// ready; at chapters, nothing — launching is the picker's call (<see cref="Launch"/>).</summary>
        public void Confirm(int slot)
        {
            switch (Screen)
            {
                case FrontendScreen.Title:
                    if (slot != 0)
                    {
                        return;
                    }

                    ChoseContinue = _canContinue && TitleCursor == 0;
                    Screen = FrontendScreen.Characters;
                    break;

                case FrontendScreen.Characters:
                    if (slot < 0 || slot >= Slots)
                    {
                        return;
                    }

                    if (!_joined[slot])
                    {
                        _joined[slot] = true;
                        return;
                    }

                    _ready[slot] = true;
                    if (EveryonePresentIsReady())
                    {
                        Screen = FrontendScreen.Chapters;
                    }

                    break;
            }
        }

        /// <summary>Heavy. Un-ready, then leave (slot 1) or go up a screen (slot 0).</summary>
        public void Back(int slot)
        {
            switch (Screen)
            {
                case FrontendScreen.Characters:
                    if (slot < 0 || slot >= Slots || !_joined[slot])
                    {
                        return;
                    }

                    if (_ready[slot])
                    {
                        _ready[slot] = false;
                    }
                    else if (slot == 0)
                    {
                        Screen = FrontendScreen.Title;
                    }
                    else
                    {
                        _joined[slot] = false;
                    }

                    break;

                case FrontendScreen.Chapters:
                    if (slot == 0)
                    {
                        for (int i = 0; i < Slots; i++)
                        {
                            _ready[i] = false;
                        }

                        Screen = FrontendScreen.Characters;
                    }

                    break;
            }
        }

        public void Launch(bool canLaunch)
        {
            if (Screen == FrontendScreen.Chapters && canLaunch)
            {
                Screen = FrontendScreen.Launching;
            }
        }

        private bool EveryonePresentIsReady()
        {
            for (int i = 0; i < Slots; i++)
            {
                if (_joined[i] && !_ready[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
