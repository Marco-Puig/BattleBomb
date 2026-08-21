using System.Collections.Generic;

namespace BattleBomb.Platform
{
    /// <summary>No disk: tests, and any dry run that must leave nothing behind.</summary>
    public sealed class MemorySaveStore : ISaveStore
    {
        private readonly Dictionary<string, string> _blobs = new Dictionary<string, string>();

        public bool Exists(string name) => _blobs.ContainsKey(Normalise(name));

        public bool TryRead(string name, out string text) => _blobs.TryGetValue(Normalise(name), out text);

        public void Write(string name, string text) => _blobs[Normalise(name)] = text ?? string.Empty;

        public void Delete(string name) => _blobs.Remove(Normalise(name));

        public IReadOnlyList<string> Names() => new List<string>(_blobs.Keys);

        /// <summary>A null or empty name is "local" (the <see cref="ISaveStore"/> contract) —
        /// matches <see cref="FileSaveStore"/> rather than silently discarding the call.</summary>
        private static string Normalise(string name) => string.IsNullOrEmpty(name) ? "local" : name;
    }
}
