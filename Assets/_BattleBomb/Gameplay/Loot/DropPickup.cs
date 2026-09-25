using BattleBomb.Core.Items;
using UnityEngine;

namespace BattleBomb.Gameplay.Loot
{
    /// <summary>
    /// A dropped item in the world (D23/D30): spawned at the corpse carrying its generated
    /// <see cref="ItemInstance"/>, tinted by quality, free for whichever player takes it first —
    /// the driver resolves grabs inside the fixed step, so a simultaneous couch dive has one
    /// deterministic winner.
    ///
    /// Its presentation is the loop made legible (M6 task 69, Michael's direction): it pops out
    /// of the corpse with a small arc instead of appearing, and it glows *from beneath* in its
    /// quality colour — subtle at the bottom of the ladder, a real light source at the top — so
    /// a good drop is readable across the arena without a beam announcing it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DropPickup : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        /// <summary>Resting height of the token above the ground.</summary>
        private const float RestHeight = 0.35f;

        /// <summary>How high the pop arcs, and how long it takes to settle.</summary>
        private const float BounceHeight = 0.85f;
        private const float BounceSeconds = 0.45f;

        /// <summary>Light range and brightness at the very bottom and top of D33's ladder.</summary>
        private const float GlowRangeLow = 1.1f;
        private const float GlowRangeHigh = 3.4f;
        private const float GlowIntensityLow = 0.5f;
        private const float GlowIntensityHigh = 5.5f;

        private ItemInstance _item;
        private Transform _token;
        private Vector3 _rest;
        private Vector3 _from;
        private float _age;
        private float _spin;

        public ItemInstance Item => _item;

        /// <summary>This drop's id on the wire, unique for the launch (HANDOFF-M8 planning decision 8).</summary>
        public int NetId { get; private set; }

        public Vector3 Position => _rest;

        internal static DropPickup Spawn(Vector3 corpse, in ItemInstance item, int netId = 0)
        {
            // The root never moves: the driver grabs from here, and the HUD projects from here.
            // Only the token child bounces and spins, so the animation is pure decoration.
            var root = new GameObject("Drop");
            root.transform.position = new Vector3(corpse.x, RestHeight, corpse.z);

            GameObject token = GameObject.CreatePrimitive(PrimitiveType.Cube);
            token.name = "Token";
            Destroy(token.GetComponent<Collider>());
            token.transform.SetParent(root.transform, false);
            token.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

            Color quality = QualityColors.For(item.Quality);
            float fraction = RankFraction(item.Quality);
            MeshRenderer renderer = token.GetComponent<MeshRenderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, quality);
            block.SetColor(EmissionColor, quality * fraction);
            renderer.SetPropertyBlock(block);

            // The glow sits *under* the item, so what the player reads is the ground lighting up
            // (Michael: "the higher tier items actually look like a source of light").
            var glow = new GameObject("Glow");
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = new Vector3(0f, -RestHeight + 0.08f, 0f);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = quality;
            light.range = Mathf.Lerp(GlowRangeLow, GlowRangeHigh, fraction);
            light.intensity = Mathf.Lerp(GlowIntensityLow, GlowIntensityHigh, fraction);
            light.shadows = LightShadows.None;

            DropPickup pickup = root.AddComponent<DropPickup>();
            pickup._item = item;
            pickup._rest = root.transform.position;
            pickup._from = new Vector3(corpse.x, corpse.y + 0.2f, corpse.z);
            pickup._token = token.transform;
            token.transform.position = pickup._from;
            pickup.NetId = netId;
            return pickup;
        }

        /// <summary>Where this rank sits on the ladder, 0 at the bottom and 1 at the top rung.</summary>
        private static float RankFraction(QualityRank rank) =>
            Mathf.Clamp01((int)rank / (float)(QualityTable.RankCount - 1));

        private void Update()
        {
            if (_token == null)
            {
                return;
            }

            _spin += Time.deltaTime * 40f;
            _token.rotation = Quaternion.Euler(45f, 45f + _spin, 0f);

            if (_age >= BounceSeconds)
            {
                _token.position = _rest;
                return;
            }

            // The pop: an arc from the corpse to the resting spot, settling as it lands.
            // Deliberately presentation-only — the simulation grabs from the root the instant
            // the drop exists, so the animation can never cost the player a pickup.
            _age = Mathf.Min(BounceSeconds, _age + Time.deltaTime);
            float t = _age / BounceSeconds;
            Vector3 flat = Vector3.Lerp(_from, _rest, t);
            flat.y += BounceHeight * Mathf.Sin(t * Mathf.PI);
            _token.position = flat;
        }
    }
}
