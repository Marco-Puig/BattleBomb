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
        public const int CurrentVersion = 1;

        /// <summary>Version N's step: takes a save at N, returns it at N+1. Version 0 is the
        /// unversioned pre-release shape; its step only stamps the number and fills the arrays.</summary>
        private static readonly Dictionary<int, Func<SaveGame, SaveGame>> Migrations =
            new Dictionary<int, Func<SaveGame, SaveGame>>
            {
                { 0, save => save.WithVersion(1) },
            };

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
