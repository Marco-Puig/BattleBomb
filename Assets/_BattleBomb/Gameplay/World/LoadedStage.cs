using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.World.Markers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleBomb.Gameplay.World
{
    /// <summary>
    /// One stage while it is in the world: the geometry scene it lives in, the run deciding its
    /// flow, the markers read out of that scene, and the props placed into it.
    /// </summary>
    /// <remarks>
    /// Through the airlock (D48) two of these exist at once, and the runner used to hold each
    /// one as a spread of loose fields plus a lossy copy for the stage ahead. Every hand-over
    /// bug lived in the gap between those two sets: a flag said "not loaded" while a scene still
    /// was, a load that finished after its launch was abandoned took over the runner anyway. A
    /// stage owning its own scene closes the gap by construction — finishing one is swapping an
    /// object, and unloading cannot skip a scene because there is no flag left to be wrong.
    /// </remarks>
    internal sealed class LoadedStage
    {
        private readonly List<ArenaMarker> _arenas = new List<ArenaMarker>();
        private readonly List<CheckpointRoomMarker> _rooms = new List<CheckpointRoomMarker>();
        private readonly List<GameObject> _props = new List<GameObject>();

        /// <summary>
        /// Where a stage scene is authored to put its first arena's left edge. Nothing at runtime
        /// depends on it — <see cref="AdoptAt"/> measures the real marker and only falls back to
        /// this — but it is the convention every stage scene is built to, and the acceptance suite
        /// holds them to it so the fixture's hand-authored numbers keep meaning what they say.
        /// It lives here, with the measurement, and <see cref="StageRunner"/> re-exports it for the
        /// tests, which cannot see an internal type.
        /// </summary>
        internal const float AuthoredFirstArenaMinX = -8f;

        internal LoadedStage(StageDefinition definition, StageRun run, int stageIndex, int generation)
        {
            Definition = definition;
            Run = run;
            StageIndex = stageIndex;
            Generation = generation;
        }

        internal StageDefinition Definition { get; }

        internal StageRun Run { get; }

        /// <summary>Where this stage sits in its chapter.</summary>
        internal int StageIndex { get; }

        /// <summary>The launch this stage belongs to. A scene that finishes loading after its
        /// launch was abandoned unloads itself instead of taking the runner over.</summary>
        internal int Generation { get; }

        internal Scene Scene { get; private set; }

        /// <summary>How far along X the scene was slid so its first arena meets the exit of the
        /// stage before it. Zero for the stage a chapter starts on.</summary>
        internal float OffsetX { get; private set; }

        /// <summary>The scene is loaded, placed, and its markers read. Until then this is only a
        /// plan: a run with nowhere to happen.</summary>
        internal bool IsReady { get; private set; }

        internal StageExitMarker Exit { get; private set; }

        internal PlayerSpawnMarker Spawn { get; private set; }

        internal int ArenaMarkerCount => _arenas.Count;

        /// <summary>Takes the loaded scene as authored, without moving it.</summary>
        internal void Adopt(Scene scene)
        {
            Scene = scene;
            CollectMarkers(scene);
            IsReady = true;
        }

        /// <summary>
        /// Adopts the scene and slides it along X so its first arena begins where the stage
        /// before it ended (D48). The offset is measured from the scene's own arena 0 rather
        /// than assumed, so a stage authored somewhere other than the usual origin still lands
        /// against the exit instead of two stages' width away from it.
        /// </summary>
        internal void AdoptAt(Scene scene, float firstArenaMinX)
        {
            Adopt(scene);
            ArenaMarker first = Arena(0);
            float authored = first != null ? first.MinX : AuthoredFirstArenaMinX;
            Shift(firstArenaMinX - authored);
        }

        internal ArenaMarker Arena(int index) =>
            index >= 0 && index < _arenas.Count ? _arenas[index] : null;

        internal CheckpointRoomMarker RoomAfter(int arenaIndex)
        {
            if (arenaIndex < 0)
            {
                return null;
            }

            for (int i = 0; i < _rooms.Count; i++)
            {
                if (_rooms[i].AfterArena == arenaIndex)
                {
                    return _rooms[i];
                }
            }

            return null;
        }

        internal TrainingDummy DummyAt(int propIndex)
        {
            if (propIndex < 0 || propIndex >= _props.Count || _props[propIndex] == null)
            {
                return null;
            }

            TrainingDummy dummy = _props[propIndex].GetComponent<TrainingDummy>();
            return dummy != null && dummy.PropIndex == propIndex ? dummy : null;
        }

        /// <summary>Puts a chest, a dummy, and where the stage asks for one a shopkeeper into
        /// every checkpoint room this stage's arenas call for (D42/D43). The scene says where
        /// they would go; the stage asset says whether they do.</summary>
        internal void SpawnProps(GameObject chest, GameObject dummy, GameObject shopkeeper)
        {
            StageSpec spec = Run.Spec;
            for (int i = 0; i < _rooms.Count; i++)
            {
                CheckpointRoomMarker room = _rooms[i];
                if (room.AfterArena < 0 || room.AfterArena >= spec.ArenaCount)
                {
                    continue;
                }

                ArenaSpec arena = spec.Arenas[room.AfterArena];
                if (!arena.CheckpointAfter)
                {
                    continue;
                }

                Place(chest, room.ChestPosition);
                GameObject placed = Place(dummy, room.DummyPosition);
                TrainingDummy target = placed != null ? placed.GetComponent<TrainingDummy>() : null;
                if (target != null)
                {
                    target.SetPropIndex(_props.Count - 1);
                }
                if (arena.ShopkeeperAfter)
                {
                    Place(shopkeeper, room.ShopkeeperPosition);
                }
            }
        }

        /// <summary>Everything this stage put into the world goes with it. Safe on a stage whose
        /// scene never finished loading: there is nothing to unload yet, and the load's own
        /// completion will find its generation stale and clean up after itself.</summary>
        internal void Unload()
        {
            for (int i = 0; i < _props.Count; i++)
            {
                if (_props[i] != null)
                {
                    Object.Destroy(_props[i]);
                }
            }

            _props.Clear();
            _arenas.Clear();
            _rooms.Clear();
            Exit = null;
            Spawn = null;
            IsReady = false;

            if (Scene.IsValid() && Scene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(Scene);
            }

            Scene = default;
        }

        /// <summary>
        /// Everything the runner reads out of this scene, read once, from this scene alone.
        /// Through the airlock two stages are loaded at once and both carry a full set of
        /// markers; an unscoped search would let the stage ahead answer for the one the players
        /// are still standing in.
        /// </summary>
        private void CollectMarkers(Scene scene)
        {
            _arenas.Clear();
            _rooms.Clear();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                _arenas.AddRange(root.GetComponentsInChildren<ArenaMarker>(true));
                _rooms.AddRange(root.GetComponentsInChildren<CheckpointRoomMarker>(true));
            }

            _arenas.Sort((a, b) => a.Index.CompareTo(b.Index));
            Exit = FindInScene<StageExitMarker>(scene);
            Spawn = FindInScene<PlayerSpawnMarker>(scene);
        }

        private void Shift(float offsetX)
        {
            if (Mathf.Approximately(offsetX, 0f))
            {
                return;
            }

            foreach (GameObject root in Scene.GetRootGameObjects())
            {
                root.transform.position += new Vector3(offsetX, 0f, 0f);
            }

            OffsetX += offsetX;
        }

        /// <summary>The marker says where on the floor a prop stands; the prefab's own Y says how
        /// far above that floor its body sits. Throwing the prefab's height away would bury a
        /// chest to its lid.</summary>
        private GameObject Place(GameObject prefab, Vector3 at)
        {
            if (prefab == null)
            {
                return null;
            }

            at.y += prefab.transform.localPosition.y;
            GameObject go = Object.Instantiate(prefab, at, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(go, Scene);
            _props.Add(go);
            return go;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
