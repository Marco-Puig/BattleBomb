using System;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Loot;
using BattleBomb.Core.Players;
using BattleBomb.Core.Saves;
using BattleBomb.Gameplay.Data;
using BattleBomb.Platform;
using UnityEngine;

namespace BattleBomb.Gameplay.Session
{
    /// <summary>
    /// What the front door hands the machine, carried across the scene change on one object
    /// that survives it: the chapter, stage, tier, and checkpoint to launch; who is playing
    /// what; the loaded save and its store. A scene-crossing object rather than a static — it
    /// is found, never assumed, and the machine without one simply runs the fixture.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSession : MonoBehaviour
    {
        public ChapterDefinition Chapter { get; set; }
        public int StageIndex { get; set; }
        public int TierIndex { get; set; }
        public int ResumeCheckpointArena { get; set; } = -1;

        /// <summary>Per couch slot; null means nobody sits there. Like <see cref="Seats"/> and
        /// <see cref="Seeds"/>, it does <em>not</em> survive a mid-play domain reload — the array re-initialises empty
        /// while Chapter, the indices, and the save come back intact — so anything reading it
        /// after a reload must check rather than assume. Nothing does today: the only readers run
        /// on a real scene load, which is the one thing a reload is not.</summary>
        public CharacterDefinition[] Characters { get; } = new CharacterDefinition[FrontendState.Slots];

        /// <summary>Which devices each couch seat owns (D57). Here because the front door hands
        /// the seats out and the machine plays on them. Like <see cref="Characters"/>, it does not
        /// survive a mid-play domain reload.</summary>
        public SeatAssignment Seats { get; } = new SeatAssignment();

        public ChapterDefinition[] Chapters { get; set; } = new ChapterDefinition[0];
        public TierDefinition[] Tiers { get; set; } = new TierDefinition[0];

        public ISaveStore Store { get; set; }
        public string SaveName { get; set; } = "local";

        /// <summary>The save as last read or written; null when there is none yet.</summary>
        public SaveGame LoadedSave { get; set; }

        /// <summary>
        /// How the last read of <see cref="SaveName"/> went. Carried rather than thrown away
        /// because <see cref="LoadedSave"/> is null for two very different reasons: no file yet,
        /// which is a fresh game and safe to write over; or a file this build refused — a save
        /// from a newer version, or a corrupt one (D52). Overwriting the second with an empty
        /// save is how a player who once opened the game with a newer build loses everything at
        /// the next checkpoint, so the autosave (task 81) has to be able to tell them apart.
        /// <see cref="SaveLoadReason.Ok"/> means "nothing refused" — including "nothing there".
        /// </summary>
        public SaveLoadReason LoadOutcome { get; set; } = SaveLoadReason.Ok;

        /// <summary>This run's seeds (F3), drawn at every launch so each run rolls its own; null
        /// before the first. Held here so the whole run — every stage, every wipe — uses one set.
        /// Like <see cref="Characters"/>, it does not survive a mid-play domain reload: after a
        /// script recompile in Play the run rolls the driver's authored constants, as every run
        /// did before F3. Editor only.</summary>
        public RunSeeds? Seeds { get; set; }

        /// <summary>Draws a fresh set for a launch. The one place a run's randomness comes from outside
        /// the game, and it is read once, before the run exists.</summary>
        public void DrawRunSeeds() => Seeds = RunSeeds.From(unchecked((uint)Guid.NewGuid().GetHashCode()));

        /// <summary>True when nothing on disk was refused, so writing cannot destroy a file we
        /// merely failed to understand.</summary>
        public bool LoadedCleanly => LoadOutcome == SaveLoadReason.Ok;

        public StoryProgress Progress { get; set; } = new StoryProgress();

        /// <summary>
        /// Reads this machine's one save (D51: one save, no profiles) and decodes it. Lives here
        /// rather than at the front door because the store is a Platform type and UI depends on
        /// Core and Gameplay only — and because the session, which outlives every scene, is the
        /// right owner of a file every scene shares. Safe to call again: a re-read is what the
        /// front door does when the player returns to it mid-run.
        /// </summary>
        public void LoadFromStore()
        {
            if (Store == null)
            {
                IPlatformServices platform = new NullPlatformServices();
                Store = platform.Saves;
                SaveName = platform.Identity.IsSignedIn ? platform.Identity.UserId : "local";
            }

            LoadedSave = null;
            LoadOutcome = SaveLoadReason.Ok;
            Progress = new StoryProgress();

            if (Store == null || !Store.TryRead(SaveName, out string text))
            {
                return;
            }

            SaveLoad load = SaveCodec.Decode(text);
            if (load.Ok)
            {
                LoadedSave = load.Save;
                Progress = SaveMapper.RestoreProgress(load.Save);
            }
            else
            {
                LoadOutcome = load.Reason;
            }
        }

        /// <summary>
        /// The one session, or null before the front door has made it. Loud about a second one:
        /// <see cref="DisallowMultipleComponentAttribute"/> stops two on one object and nothing
        /// stops two on two objects, and <c>FindAnyObjectByType</c> would then hand back an
        /// arbitrary one of them. Everything downstream — the launch request, the loaded save,
        /// who is on the couch — reads whichever it got, so a duplicate is not a tie to be broken
        /// quietly but a bug that has already started returning different answers to different
        /// callers. The extras go and the fact is reported, the way every other bad assumption in
        /// this scene's boot is reported.
        /// </summary>
        public static GameSession Find()
        {
            GameSession[] found = FindObjectsByType<GameSession>(FindObjectsSortMode.None);
            if (found.Length == 0)
            {
                return null;
            }

            for (int i = 1; i < found.Length; i++)
            {
                Debug.LogError(
                    $"{found[i].name}: a second GameSession. There is one session per machine (D51) " +
                    "and it carries the launch request and the loaded save across the scene change; " +
                    "two means callers disagree about both. Destroying this one and keeping " +
                    $"'{found[0].name}' — which of them survives is arbitrary, which is the point.",
                    found[i]);
                Destroy(found[i]);
            }

            return found[0];
        }


        public static GameSession FindOrCreate()
        {
            GameSession existing = Find();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("Game Session");
            DontDestroyOnLoad(go);
            return go.AddComponent<GameSession>();
        }

        public int PresentPlayers
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Characters.Length; i++)
                {
                    if (Characters[i] != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
