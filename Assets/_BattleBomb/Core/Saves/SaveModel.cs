using System;
using UnityEngine;

namespace BattleBomb.Core.Saves
{
    /// <summary>
    /// The save file's shape (D52), as plain serialisable data. These are DTOs for
    /// <c>JsonUtility</c>, which needs fields — so this is the one corner of Core with mutable
    /// private fields, built by constructor and read through properties. Nothing here has
    /// behaviour; <see cref="SaveMapper"/> converts, <see cref="SaveCodec"/> serialises.
    /// </summary>
    [Serializable]
    public sealed class SaveGame
    {
        [SerializeField] private int _version;
        [SerializeField] private int _coins;
        [SerializeField] private bool _autoEquip;
        [SerializeField] private bool _autoSell;
        [SerializeField] private ItemStackSave[] _sack;
        [SerializeField] private CharacterSave[] _characters;
        [SerializeField] private StoryProgressSave _story;

        public SaveGame()
        {
        }

        public SaveGame(
            int version, int coins, bool autoEquip, bool autoSell,
            ItemStackSave[] sack, CharacterSave[] characters, StoryProgressSave story)
        {
            _version = version;
            _coins = coins;
            _autoEquip = autoEquip;
            _autoSell = autoSell;
            _sack = sack ?? Array.Empty<ItemStackSave>();
            _characters = characters ?? Array.Empty<CharacterSave>();
            _story = story ?? new StoryProgressSave();
        }

        public static SaveGame Fresh() => new SaveGame(
            SaveCodec.CurrentVersion, 0, false, false,
            Array.Empty<ItemStackSave>(), Array.Empty<CharacterSave>(), new StoryProgressSave());

        public int Version => _version;
        public int Coins => _coins;
        public bool AutoEquip => _autoEquip;
        public bool AutoSell => _autoSell;
        public ItemStackSave[] Sack => _sack ?? Array.Empty<ItemStackSave>();
        public CharacterSave[] Characters => _characters ?? Array.Empty<CharacterSave>();
        public StoryProgressSave Story => _story ?? new StoryProgressSave();

        /// <summary>The same save stamped with a version — what a migration step returns.</summary>
        public SaveGame WithVersion(int version) =>
            new SaveGame(version, _coins, _autoEquip, _autoSell, Sack, Characters, Story);
    }

    [Serializable]
    public sealed class ItemStackSave
    {
        [SerializeField] private ItemSave _item;
        [SerializeField] private int _count;

        public ItemStackSave()
        {
        }

        public ItemStackSave(ItemSave item, int count)
        {
            _item = item;
            _count = count;
        }

        public ItemSave Item => _item;
        public int Count => _count;
    }

    /// <summary>Every rolled value on an instance (D35: stored, never re-derived). The name is
    /// kept only as a fallback for an item whose definition no longer exists.</summary>
    [Serializable]
    public sealed class ItemSave
    {
        [SerializeField] private int _definitionId;
        [SerializeField] private string _name;
        [SerializeField] private int _slot;
        [SerializeField] private int _weaponClass;
        [SerializeField] private int _petClass;
        [SerializeField] private int _quality;
        [SerializeField] private GearContributionSave _coreStats;
        [SerializeField] private AffixSave[] _affixes;
        [SerializeField] private int _requiredLevel;
        [SerializeField] private int _capacity;
        [SerializeField] private int _spent;
        [SerializeField] private bool _locked;
        [SerializeField] private float _shotSpeed;
        [SerializeField] private int _restoreKind;
        [SerializeField] private float _restoreFraction;
        [SerializeField] private float _activeShare;
        [SerializeField] private int _activeElement;
        [SerializeField] private float _activeRadius;
        [SerializeField] private int _activeCooldown;

        public ItemSave()
        {
        }

        public ItemSave(
            int definitionId, string name, int slot, int weaponClass, int petClass, int quality,
            GearContributionSave coreStats, AffixSave[] affixes, int requiredLevel, int capacity, int spent, bool locked,
            float shotSpeed, int restoreKind, float restoreFraction,
            float activeShare, int activeElement, float activeRadius, int activeCooldown)
        {
            _definitionId = definitionId;
            _name = name ?? string.Empty;
            _slot = slot;
            _weaponClass = weaponClass;
            _petClass = petClass;
            _quality = quality;
            _coreStats = coreStats ?? new GearContributionSave();
            _affixes = affixes ?? Array.Empty<AffixSave>();
            _requiredLevel = requiredLevel;
            _capacity = capacity;
            _spent = spent;
            _locked = locked;
            _shotSpeed = shotSpeed;
            _restoreKind = restoreKind;
            _restoreFraction = restoreFraction;
            _activeShare = activeShare;
            _activeElement = activeElement;
            _activeRadius = activeRadius;
            _activeCooldown = activeCooldown;
        }

        public int DefinitionId => _definitionId;
        public string Name => _name ?? string.Empty;
        public int Slot => _slot;
        public int WeaponClass => _weaponClass;
        public int PetClass => _petClass;
        public int Quality => _quality;
        public GearContributionSave CoreStats => _coreStats ?? new GearContributionSave();
        public AffixSave[] Affixes => _affixes ?? Array.Empty<AffixSave>();
        public int RequiredLevel => _requiredLevel;
        public int Capacity => _capacity;
        public int Spent => _spent;
        public bool Locked => _locked;
        public float ShotSpeed => _shotSpeed;
        public int RestoreKind => _restoreKind;
        public float RestoreFraction => _restoreFraction;
        public float ActiveShare => _activeShare;
        public int ActiveElement => _activeElement;
        public float ActiveRadius => _activeRadius;
        public int ActiveCooldown => _activeCooldown;

        /// <summary>A default-constructed entry — JsonUtility's stand-in for "nothing here".</summary>
        public bool IsEmpty => _definitionId == 0;
    }

    /// <summary>
    /// <see cref="BattleBomb.Core.Stats.GearContribution"/>'s fourteen fields, named rather than
    /// positional (D52 review): a reorder of either this class or <c>GearContribution</c>'s own
    /// constructor is then a compile error or an obviously wrong name, never a silent transposed
    /// stat on every saved item. A field JsonUtility cannot find on an older save reads as the
    /// same 0f a missing array slot used to give.
    /// </summary>
    [Serializable]
    public sealed class GearContributionSave
    {
        [SerializeField] private float _weaponDamage;
        [SerializeField] private float _swingSpeedBonus;
        [SerializeField] private float _defence;
        [SerializeField] private float _weight;
        [SerializeField] private float _critChance;
        [SerializeField] private float _critDamageBonus;
        [SerializeField] private float _lifeSteal;
        [SerializeField] private float _maxHealthBonus;
        [SerializeField] private float _maxManaBonus;
        [SerializeField] private float _manaRegen;
        [SerializeField] private float _weightReduction;
        [SerializeField] private float _knockbackBonus;
        [SerializeField] private float _magicDamage;
        [SerializeField] private float _magicRange;

        public GearContributionSave()
        {
        }

        public GearContributionSave(
            float weaponDamage, float swingSpeedBonus, float defence, float weight,
            float critChance, float critDamageBonus, float lifeSteal, float maxHealthBonus,
            float maxManaBonus, float manaRegen, float weightReduction, float knockbackBonus,
            float magicDamage, float magicRange)
        {
            _weaponDamage = weaponDamage;
            _swingSpeedBonus = swingSpeedBonus;
            _defence = defence;
            _weight = weight;
            _critChance = critChance;
            _critDamageBonus = critDamageBonus;
            _lifeSteal = lifeSteal;
            _maxHealthBonus = maxHealthBonus;
            _maxManaBonus = maxManaBonus;
            _manaRegen = manaRegen;
            _weightReduction = weightReduction;
            _knockbackBonus = knockbackBonus;
            _magicDamage = magicDamage;
            _magicRange = magicRange;
        }

        public float WeaponDamage => _weaponDamage;
        public float SwingSpeedBonus => _swingSpeedBonus;
        public float Defence => _defence;
        public float Weight => _weight;
        public float CritChance => _critChance;
        public float CritDamageBonus => _critDamageBonus;
        public float LifeSteal => _lifeSteal;
        public float MaxHealthBonus => _maxHealthBonus;
        public float MaxManaBonus => _maxManaBonus;
        public float ManaRegen => _manaRegen;
        public float WeightReduction => _weightReduction;
        public float KnockbackBonus => _knockbackBonus;
        public float MagicDamage => _magicDamage;
        public float MagicRange => _magicRange;
    }

    [Serializable]
    public sealed class AffixSave
    {
        [SerializeField] private int _id;
        [SerializeField] private float _magnitude;
        [SerializeField] private int _element;

        public AffixSave()
        {
        }

        public AffixSave(int id, float magnitude, int element)
        {
            _id = id;
            _magnitude = magnitude;
            _element = element;
        }

        public int Id => _id;
        public float Magnitude => _magnitude;
        public int Element => _element;
    }

    /// <summary>One roster character (D51): keyed by its element, since D46's roster is the
    /// elements. XP, the allocation, prestige, what is worn, and the quick slot.</summary>
    [Serializable]
    public sealed class CharacterSave
    {
        [SerializeField] private int _elementId;
        [SerializeField] private int _level;
        [SerializeField] private float _xpIntoLevel;
        [SerializeField] private int _unspentPoints;
        [SerializeField] private int _strength;
        [SerializeField] private int _hp;
        [SerializeField] private int _mana;
        [SerializeField] private int _speed;
        [SerializeField] private int _prestigeCount;
        [SerializeField] private WornSave[] _worn;
        [SerializeField] private int _quickKind;
        [SerializeField] private int _quickConsumableId;
        [SerializeField] private int _quickEquipmentIndex;

        public CharacterSave()
        {
        }

        public CharacterSave(
            int elementId, int level, float xpIntoLevel, int unspentPoints,
            int strength, int hp, int mana, int speed, int prestigeCount,
            WornSave[] worn, int quickKind, int quickConsumableId, int quickEquipmentIndex)
        {
            _elementId = elementId;
            _level = level;
            _xpIntoLevel = xpIntoLevel;
            _unspentPoints = unspentPoints;
            _strength = strength;
            _hp = hp;
            _mana = mana;
            _speed = speed;
            _prestigeCount = prestigeCount;
            _worn = worn ?? Array.Empty<WornSave>();
            _quickKind = quickKind;
            _quickConsumableId = quickConsumableId;
            _quickEquipmentIndex = quickEquipmentIndex;
        }

        public int ElementId => _elementId;
        public int Level => _level;
        public float XpIntoLevel => _xpIntoLevel;
        public int UnspentPoints => _unspentPoints;
        public int Strength => _strength;
        public int Hp => _hp;
        public int Mana => _mana;
        public int Speed => _speed;
        public int PrestigeCount => _prestigeCount;
        public WornSave[] Worn => _worn ?? Array.Empty<WornSave>();
        public int QuickKind => _quickKind;
        public int QuickConsumableId => _quickConsumableId;
        public int QuickEquipmentIndex => _quickEquipmentIndex;
    }

    [Serializable]
    public sealed class WornSave
    {
        [SerializeField] private int _slot;
        [SerializeField] private int _equipmentIndex;
        [SerializeField] private ItemSave _item;

        public WornSave()
        {
        }

        public WornSave(int slot, int equipmentIndex, ItemSave item)
        {
            _slot = slot;
            _equipmentIndex = equipmentIndex;
            _item = item;
        }

        public int Slot => _slot;
        public int EquipmentIndex => _equipmentIndex;
        public ItemSave Item => _item;
    }

    [Serializable]
    public sealed class StoryProgressSave
    {
        [SerializeField] private ChapterProgressSave[] _chapters;
        [SerializeField] private string _resumeChapterId;
        [SerializeField] private int _resumeStageIndex;
        [SerializeField] private int _resumeCheckpointArena = -1;

        public StoryProgressSave()
        {
        }

        public StoryProgressSave(ChapterProgressSave[] chapters, string resumeChapterId, int resumeStageIndex, int resumeCheckpointArena)
        {
            _chapters = chapters ?? Array.Empty<ChapterProgressSave>();
            _resumeChapterId = resumeChapterId ?? string.Empty;
            _resumeStageIndex = resumeStageIndex;
            _resumeCheckpointArena = resumeCheckpointArena;
        }

        public ChapterProgressSave[] Chapters => _chapters ?? Array.Empty<ChapterProgressSave>();
        public string ResumeChapterId => _resumeChapterId ?? string.Empty;
        public int ResumeStageIndex => _resumeStageIndex;
        public int ResumeCheckpointArena => _resumeCheckpointArena;
    }

    [Serializable]
    public sealed class ChapterProgressSave
    {
        [SerializeField] private string _chapterId;
        [SerializeField] private int _tiersBeaten;

        public ChapterProgressSave()
        {
        }

        public ChapterProgressSave(string chapterId, int tiersBeaten)
        {
            _chapterId = chapterId ?? string.Empty;
            _tiersBeaten = tiersBeaten;
        }

        public string ChapterId => _chapterId ?? string.Empty;
        public int TiersBeaten => _tiersBeaten;
    }
}
