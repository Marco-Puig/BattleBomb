using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Saves
{
    public enum SaveLoadReason
    {
        Ok = 0,
        Corrupt = 1,
        NewerVersion = 2,
    }

    public readonly struct SaveLoad
    {
        public readonly SaveGame Save;
        public readonly SaveLoadReason Reason;

        public SaveLoad(SaveGame save, SaveLoadReason reason)
        {
            Save = save;
            Reason = reason;
        }

        public bool Ok => Reason == SaveLoadReason.Ok && Save != null;
    }

    /// <summary>
    /// Text in, text out, with a version envelope (D52). Older saves walk the migration table
    /// forward one version at a time; a save from a newer build is refused, because guessing
    /// at a shape we have never seen is how a player's inventory gets silently emptied.
    /// <c>JsonUtility</c> is allowed in Core: it needs no scene, so this is all EditMode-testable.
    /// </summary>
    public static class SaveCodec
    {
        public const int CurrentVersion = 2;

        /// <summary>
        /// D33's ladder gained a rung and swapped two names, so every rank an older save stored
        /// as a bare int now points at the wrong rung. Old index → new index; a rank this table
        /// does not name is left where it is rather than guessed at.
        /// </summary>
        private static readonly int[] LadderV1ToV2 =
        {
            0,  // Nothing        → Nothing
            1,  // Battlescarred  → Battlescarred
            3,  // Torn           → Torn      (was 2, the ladder now runs Rusty before Torn)
            2,  // Rusty          → Rusty     (was 3)
            5,  // Shiny          → Shiny     (Clean was inserted at 4)
            6,  // Pristine       → Pristine
            7,  // Legendary      → Legendary
            8,  // Mythical       → Mythical
            9,  // Godly          → Godly     (now reserved above the launch ladder)
        };

        /// <summary>Version N's step: takes a save at N, returns it at N+1. Version 0 is the
        /// unversioned pre-release shape; its step only stamps the number and fills the arrays.</summary>
        private static readonly Dictionary<int, Func<SaveGame, SaveGame>> Migrations =
            new Dictionary<int, Func<SaveGame, SaveGame>>
            {
                { 0, save => save.WithVersion(1) },
                { 1, RemapLadder },
            };

        /// <summary>
        /// Rewrites every stored rank through <see cref="LadderV1ToV2"/> — the sack's stacks and
        /// each character's worn pieces alike. Without this a saved Shiny reads back as a Clean.
        /// </summary>
        private static SaveGame RemapLadder(SaveGame save)
        {
            ItemStackSave[] sack = save.Sack;
            var remappedSack = new ItemStackSave[sack.Length];
            for (int i = 0; i < sack.Length; i++)
            {
                remappedSack[i] = new ItemStackSave(Remap(sack[i]?.Item), sack[i]?.Count ?? 0);
            }

            CharacterSave[] characters = save.Characters;
            var remappedCharacters = new CharacterSave[characters.Length];
            for (int i = 0; i < characters.Length; i++)
            {
                CharacterSave character = characters[i];
                if (character == null)
                {
                    continue;
                }

                WornSave[] worn = character.Worn;
                var remappedWorn = new WornSave[worn.Length];
                for (int w = 0; w < worn.Length; w++)
                {
                    remappedWorn[w] = new WornSave(
                        worn[w]?.Slot ?? 0, worn[w]?.EquipmentIndex ?? 0, Remap(worn[w]?.Item));
                }

                remappedCharacters[i] = character.WithWorn(remappedWorn);
            }

            return new SaveGame(
                2, save.Coins, save.AutoEquip, save.AutoSell,
                remappedSack, remappedCharacters, save.Story);
        }

        private static ItemSave Remap(ItemSave item)
        {
            if (item == null)
            {
                return null;
            }

            int quality = item.Quality;
            return quality >= 0 && quality < LadderV1ToV2.Length
                ? item.WithQuality(LadderV1ToV2[quality])
                : item;
        }

        public static string Encode(SaveGame save) =>
            JsonUtility.ToJson(save ?? SaveGame.Fresh(), prettyPrint: false);

        public static SaveLoad Decode(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new SaveLoad(null, SaveLoadReason.Corrupt);
            }

            SaveGame save;
            try
            {
                save = JsonUtility.FromJson<SaveGame>(text);
            }
            catch (ArgumentException)
            {
                return new SaveLoad(null, SaveLoadReason.Corrupt);
            }

            if (save == null)
            {
                return new SaveLoad(null, SaveLoadReason.Corrupt);
            }

            if (save.Version > CurrentVersion)
            {
                return new SaveLoad(null, SaveLoadReason.NewerVersion);
            }

            while (save.Version < CurrentVersion)
            {
                if (!Migrations.TryGetValue(save.Version, out Func<SaveGame, SaveGame> step))
                {
                    return new SaveLoad(null, SaveLoadReason.Corrupt);
                }

                save = step(save);
            }

            return new SaveLoad(save, SaveLoadReason.Ok);
        }
    }
}
