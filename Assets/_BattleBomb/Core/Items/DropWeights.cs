namespace BattleBomb.Core.Items
{
    /// <summary>
    /// What kind of thing falls when something drops — authored weights per slot (task 41 paper:
    /// consumables common, armor next, weapons, equipment, pets rarest). Slots absent from the
    /// catalog simply never win; the remaining weights re-normalize by construction.
    /// </summary>
    public readonly struct DropWeights
    {
        public readonly float Helmet;
        public readonly float Chest;
        public readonly float Boots;
        public readonly float Weapon;
        public readonly float Pet;
        public readonly float Equipment;
        public readonly float Consumable;

        public DropWeights(float helmet, float chest, float boots, float weapon, float pet, float equipment, float consumable)
        {
            Helmet = helmet;
            Chest = chest;
            Boots = boots;
            Weapon = weapon;
            Pet = pet;
            Equipment = equipment;
            Consumable = consumable;
        }

        public static DropWeights Default => new DropWeights(
            helmet: 11f, chest: 11f, boots: 11f, weapon: 15f, pet: 2f, equipment: 8f, consumable: 30f);

        public float For(ItemSlot slot)
        {
            switch (slot)
            {
                case ItemSlot.Helmet: return Helmet;
                case ItemSlot.Chest: return Chest;
                case ItemSlot.Boots: return Boots;
                case ItemSlot.Weapon: return Weapon;
                case ItemSlot.Pet: return Pet;
                case ItemSlot.Equipment: return Equipment;
                case ItemSlot.Consumable: return Consumable;
                default: return 0f;
            }
        }
    }
}
