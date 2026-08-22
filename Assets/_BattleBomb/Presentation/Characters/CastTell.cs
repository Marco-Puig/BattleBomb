using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Presentation.Characters
{
    /// <summary>
    /// Placeholder cast VFX (M5): the splash's line erupting ahead, the aura's ring, and the leap's
    /// puff, drawn in the element's own colour so a new element looks like itself with no code
    /// change (D38). Shapes only — the real art arrives with the roster. Observational: it reads
    /// the actor's combat state and never writes a thing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CastTell : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [Tooltip("Driver whose casts are observed. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [SerializeField] private float _lifetimeSeconds = 0.28f;

        private struct Puff
        {
            public Transform Body;
            public float Born;
            public Vector3 Scale;
        }

        private readonly List<Puff> _live = new List<Puff>();
        private readonly Dictionary<int, MagicCastKind> _casting = new Dictionary<int, MagicCastKind>();
        private MaterialPropertyBlock _block;
        private Material _material;

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            _block = new MaterialPropertyBlock();
            if (_driver != null)
            {
                _driver.Stepped += OnStepped;
            }
        }

        private void OnDisable()
        {
            if (_driver != null)
            {
                _driver.Stepped -= OnStepped;
            }
        }

        /// <summary>
        /// Watches every player for a cast that has just entered its active phase — the frame the
        /// spell exists — and puffs a shape where it resolves.
        /// </summary>
        private void OnStepped(int frame)
        {
            IReadOnlyList<CharacterActor> players = _driver.Characters.Ordered;
            for (int i = 0; i < players.Count; i++)
            {
                CharacterActor player = players[i];
                int id = player.PlayerId.Value;
                MagicCastKind now = player.ActiveCast;
                _casting.TryGetValue(id, out MagicCastKind previous);
                _casting[id] = now;

                if (now == MagicCastKind.None || now == previous)
                {
                    continue;
                }

                Spawn(player, now);
            }
        }

        private void Spawn(CharacterActor player, MagicCastKind kind)
        {
            AttackTuning cast = player.CurrentCastTuning;
            Vector3 centre = player.Position + Vector3.up * 0.6f;
            Vector3 scale;

            if (cast.IsRadial)
            {
                scale = new Vector3(cast.ReachX * 2f, 0.5f, cast.ReachX * 2f);
            }
            else
            {
                float facing = player.Facing == Core.Movement.Facing.Right ? 1f : -1f;
                centre += new Vector3(facing * cast.ReachX * 0.5f, 0f, 0f);
                scale = new Vector3(cast.ReachX, 0.9f, cast.DepthTolerance * 2f);
            }

            GameObject body = GameObject.CreatePrimitive(
                cast.IsRadial ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(transform, false);
            body.transform.position = centre;
            body.transform.localScale = cast.IsRadial
                ? new Vector3(scale.x, 0.05f, scale.z)
                : scale;

            Renderer skin = body.GetComponent<Renderer>();
            if (_material == null)
            {
                _material = new Material(skin.sharedMaterial);
            }

            skin.sharedMaterial = _material;

            _live.Add(new Puff
            {
                Body = body.transform,
                Born = Time.time,
                Scale = body.transform.localScale,
            });

            _block.SetColor(BaseColor, _driver.ColorOf(player.Element));
            skin.SetPropertyBlock(_block);
        }

        private void LateUpdate()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                float age = Time.time - _live[i].Born;
                if (age >= _lifetimeSeconds || _live[i].Body == null)
                {
                    if (_live[i].Body != null)
                    {
                        Destroy(_live[i].Body.gameObject);
                    }

                    _live.RemoveAt(i);
                    continue;
                }

                // The whole tell is the expansion: the shape blooms to its true size and vanishes,
                // which reads at speed without depending on transparency the placeholder material
                // does not have.
                float t = age / _lifetimeSeconds;
                _live[i].Body.localScale = _live[i].Scale * (0.6f + 0.6f * t);
            }
        }
    }
}
