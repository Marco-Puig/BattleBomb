using BattleBomb.Core.Chapters;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using UnityEngine;

namespace BattleBomb.UI.Debug
{
    /// <summary>
    /// The numbers the player is never shown (D50): the difficulty row the run launched on, the
    /// stage's own dials, and the effective encounter the two multiply out to. D50 hides every
    /// multiplier behind a tier <em>name</em> on purpose — a player choosing "Hard" should be
    /// choosing a promise, not reading a spreadsheet — but somebody still has to be able to see
    /// whether the promise is being kept, and no other surface in the game reports it.
    /// </summary>
    /// <remarks>
    /// The class <em>declaration</em> is outside <c>#if DEVELOPMENT_BUILD || UNITY_EDITOR</c> and
    /// its entire body is inside it, so a release build ships this as an empty stub: no fields, no
    /// <c>OnGUI</c>, nothing to switch on and nothing to read. The stub is what keeps the Gameplay
    /// scene's serialised reference to this component resolvable; the numbers still cannot leak,
    /// because the only thing that can reveal them — <c>SettingsMenu</c>'s overlay row, and the
    /// field and lookup behind it — compiles out behind the same guard.
    /// <para>
    /// The effective row is read back off <see cref="SimulationDriver.Encounter"/> rather than
    /// recomputed here from <see cref="EncounterInputs.From"/>. Both print the same numbers today,
    /// and that is exactly why the driver is the one to ask: a tool whose whole job is checking
    /// that the tier's promise is being kept has to report the encounter the machine is actually
    /// rolling against, not a second copy of the same sum — which would agree with itself even on
    /// the day nothing reached the driver at all.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TierOverlay : MonoBehaviour
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        [Tooltip("The machine's driver. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("The runner whose tier and stage these numbers are. Leave empty to find it.")]
        [SerializeField] private StageRunner _runner;

        /// <summary>Six rows of it, at a fixed height, so the box can be placed before the text
        /// is built.</summary>
        private const float RowHeight = 22f;
        private const float BoxWidth = 470f;
        private const int Rows = 6;

        private GUIStyle _style;
        private float _top;

        /// <summary>Off until somebody asks for it, from settings → debug (D50).</summary>
        public bool Visible { get; set; }

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_runner == null)
            {
                _runner = FindAnyObjectByType<StageRunner>();
            }
        }

        private void OnGUI()
        {
            if (!Visible || _runner == null)
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 15 };
                _style.normal.textColor = Color.white;
            }

            TierSpec tier = _runner.Tier;
            StageRun run = _runner.Run;

            // Before a launch there is no stage, so there is no encounter to report — saying
            // "level 1" there would be a number nobody chose.
            bool running = run != null;

            // Bottom-left, not top-left, because CommandDebugOverlay already owns the top-left
            // corner and the health bars own the top-middle. Drawn over the tier row, the one
            // line that is the whole point of this overlay is the line you cannot read.
            float height = Rows * RowHeight + 14f;
            _top = Screen.height - 12f - height;
            GUI.Box(new Rect(12f, _top, BoxWidth, height), GUIContent.none);
            Row(0, "TIER (dev) — " + (tier.Name.Length > 0 ? tier.Name : "?"));
            Row(1, $"Tier row: HP x{tier.HealthMultiplier:0.##}  DMG x{tier.DamageMultiplier:0.##}"
                + $"  +{tier.LevelBump} lv  loot x{tier.LootMultiplier:0.##}");

            if (!running)
            {
                Row(2, "No stage running.");
                return;
            }

            Row(2, $"Stage {_runner.StageIndex + 1} '{run.Spec.Id}'  arena {run.ArenaIndex + 1}"
                + $"/{run.Spec.ArenaCount}  phase {run.Phase}");
            Row(5, $"Alive {run.Alive}  checkpoint arena {run.CheckpointArena}"
                + $"  airlock {(run.IsAirlock ? "yes" : "no")}");

            if (_driver == null)
            {
                Row(3, "No SimulationDriver, so what the machine is actually rolling against "
                    + "cannot be read.");
                return;
            }

            EncounterInputs effective = _driver.Encounter;
            Row(3, $"Effective: level stamp {effective.LevelStamp}"
                + $"  loot progress {effective.LootProgress:0.##} x {effective.LootDifficulty:0.##}");
            Row(4, $"Enemies: HP x{effective.HealthMultiplier:0.##}  DMG x{effective.DamageMultiplier:0.##}");
        }

        private void Row(int index, string text) =>
            GUI.Label(new Rect(20f, _top + 6f + index * RowHeight, BoxWidth - 16f, 20f), text, _style);
#endif
    }
}
