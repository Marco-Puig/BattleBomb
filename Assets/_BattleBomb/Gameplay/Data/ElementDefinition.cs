using System.Collections.Generic;
using BattleBomb.Core.Combat;
using UnityEngine;

namespace BattleBomb.Gameplay.Data
{
    /// <summary>
    /// Authoring format for one element (D38) — the whole reason the roster (O11) is a content
    /// question. A new element is this asset and nothing else: no enum entry, no switch statement,
    /// no code change anywhere. Status behaviour joins this asset when M5 gives elements teeth.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleBomb/Element Definition", fileName = "ElementDefinition")]
    public sealed class ElementDefinition : ScriptableObject
    {
        [Tooltip("Stable id, unique across the catalog. Never 0 — that is reserved for None.")]
        [SerializeField] private int _id = 1;

        [Tooltip("What the player reads. Empty falls back to the asset's own name.")]
        [SerializeField] private string _displayName = string.Empty;

        [Tooltip("The element's colour, for Presentation — tints, statuses, cast VFX.")]
        [SerializeField] private Color _color = new Color(1f, 0.45f, 0.15f);

        [Header("Status (D40) — the mark this element leaves")]
        [Tooltip("What the player calls it: Burn, Soak, Shock. Empty means this element marks nothing.")]
        [SerializeField] private string _statusName = string.Empty;

        [Tooltip("How long the mark lasts, in simulation steps. 0 means no status at all.")]
        [SerializeField] private int _statusDurationSteps = 180;

        [Tooltip("Steps between damage ticks.")]
        [SerializeField] private int _statusTickSteps = 30;

        [Tooltip("Total damage over the mark's life, as a share of the hit that applied it.")]
        [SerializeField] private float _statusDamageShare = 0.5f;

        [Tooltip("Movement multiplier while marked (D46) — 1 leaves movement alone; Chill authors 0.55.")]
        [SerializeField] private float _statusMoveScale = 1f;

        [Header("Signature cast (D46) — the press every character of this element shares")]
        [Tooltip("Off means no signature: characters keep whatever their own press cast authors.")]
        [SerializeField] private bool _hasSignature;

        [Tooltip("The signature's frame data and shape (stun steps included — Earth's wall).")]
        [SerializeField] private AttackSpec _signature;

        [Tooltip("Mana the signature press costs.")]
        [SerializeField] private int _signatureManaCost = 20;

        [Tooltip("Hitbox resolves a shape in front; Projectile fires a bolt down the caster's lane (Ice).")]
        [SerializeField] private CastDelivery _signatureDelivery = CastDelivery.Hitbox;

        [Tooltip("Bolt speed in units per second — projectile delivery only.")]
        [SerializeField] private float _signatureBoltSpeed = 14f;

        public ElementId Id => new ElementId(_id);

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        public Color Color => _color;

        public ElementSpec ToRuntime() => new ElementSpec(Id, DisplayName, ToStatus(), ToSignature());

        private StatusSpec ToStatus() => string.IsNullOrEmpty(_statusName)
            ? default
            : new StatusSpec(
                _statusName, _statusDurationSteps, _statusTickSteps, _statusDamageShare,
                _statusMoveScale);

        private MagicCast ToSignature() => !_hasSignature || _signature == null
            ? default
            : new MagicCast(
                _signature.ToRuntime(), _signatureManaCost, 0f, _signatureDelivery,
                _signatureBoltSpeed);

        /// <summary>Builds the runtime catalog from an authored list, skipping empty slots.</summary>
        public static ElementCatalog ToCatalog(IReadOnlyList<ElementDefinition> definitions)
        {
            if (definitions == null || definitions.Count == 0)
            {
                return ElementCatalog.Empty;
            }

            var specs = new List<ElementSpec>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && !definitions[i].Id.IsNone)
                {
                    specs.Add(definitions[i].ToRuntime());
                }
            }

            return new ElementCatalog(specs);
        }
    }

    /// <summary>
    /// One authored row of an elemental table — an enemy's resistance to an element, an area's
    /// climate toward it (§4). Rows nobody authors stay neutral.
    /// </summary>
    [System.Serializable]
    public sealed class ElementMultiplierSpec
    {
        [SerializeField] private ElementDefinition _element;

        [Tooltip("1 is neutral; below 1 resists, above 1 amplifies.")]
        [SerializeField] private float _multiplier = 1f;

        public ElementalMultiplier ToRuntime() => new ElementalMultiplier(
            _element != null ? _element.Id : ElementId.None, Mathf.Max(0f, _multiplier));

        public static ElementalMultipliers ToTable(IReadOnlyList<ElementMultiplierSpec> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return ElementalMultipliers.Neutral;
            }

            var entries = new List<ElementalMultiplier>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    entries.Add(rows[i].ToRuntime());
                }
            }

            return ElementalMultipliers.From(entries);
        }
    }
}
