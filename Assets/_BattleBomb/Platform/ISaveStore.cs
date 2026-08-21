using System.Collections.Generic;

namespace BattleBomb.Platform
{
    /// <summary>
    /// Named text blobs (D52). Core never touches a disk; it hands text to one of these. The
    /// file store is the default everywhere; Steam Cloud becomes a second implementation
    /// behind this interface when the Steamworks layer arrives, with zero Core changes.
    /// A null or empty <c>name</c> means the one local save: every implementation treats it
    /// as <c>"local"</c>, so a caller with no platform id still has somewhere to save.
    /// </summary>
    public interface ISaveStore
    {
        bool Exists(string name);

        bool TryRead(string name, out string text);

        void Write(string name, string text);

        void Delete(string name);

        IReadOnlyList<string> Names();
    }
}
