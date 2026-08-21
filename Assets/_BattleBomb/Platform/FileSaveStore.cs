using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BattleBomb.Platform
{
    /// <summary>
    /// Saves as files in one folder. Writes land in a temp file first, then
    /// <see cref="File.Replace(string, string, string)"/> swaps it over the previous save as one
    /// atomic OS call — the crash window a plain delete-then-move would leave is closed, so a
    /// crash before or during the swap always leaves either the old save or the new one intact,
    /// never neither. The very first save has no destination yet, so it falls back to a plain
    /// move. Names are mangled to a safe character set so a platform user id can never path out
    /// of the folder.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        private const string Extension = ".save";
        private readonly string _directory;

        public FileSaveStore(string directory)
        {
            _directory = directory;
        }

        public bool Exists(string name) => File.Exists(PathFor(name));

        public bool TryRead(string name, out string text)
        {
            string path = PathFor(name);
            if (!File.Exists(path))
            {
                text = null;
                return false;
            }

            text = File.ReadAllText(path, Encoding.UTF8);
            return true;
        }

        public void Write(string name, string text)
        {
            Directory.CreateDirectory(_directory);
            string path = PathFor(name);
            string temp = path + ".tmp";
            File.WriteAllText(temp, text ?? string.Empty, Encoding.UTF8);

            if (File.Exists(path))
            {
                // One atomic OS call: no window where neither file exists.
                File.Replace(temp, path, null);
            }
            else
            {
                // Replace requires an existing destination; the first-ever save has none.
                File.Move(temp, path);
            }
        }

        public void Delete(string name)
        {
            string path = PathFor(name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public IReadOnlyList<string> Names()
        {
            var names = new List<string>();
            if (!Directory.Exists(_directory))
            {
                return names;
            }

            foreach (string file in Directory.GetFiles(_directory, "*" + Extension))
            {
                names.Add(Path.GetFileNameWithoutExtension(file));
            }

            return names;
        }

        private string PathFor(string name) => Path.Combine(_directory, Sanitise(name) + Extension);

        /// <summary>A null or empty name is "local" (the <see cref="ISaveStore"/> contract).
        /// Every other character not alphanumeric, '-', or '_' also collapses to '_' — so two
        /// distinct raw names that differ only in punctuation collide on one file. Not reachable
        /// today (D51: one save per machine; Steam ids are plain digits), but the next identity
        /// source that isn't purely alphanumeric needs to know this before it collides.</summary>
        private static string Sanitise(string name)
        {
            var builder = new StringBuilder((name ?? "local").Length);
            foreach (char c in name ?? "local")
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            }

            return builder.Length > 0 ? builder.ToString() : "local";
        }
    }
}
