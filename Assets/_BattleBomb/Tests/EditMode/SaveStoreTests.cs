using System.IO;
using BattleBomb.Platform;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>D52: named text blobs behind the Platform seam. The file store is the default;
    /// the memory store is what tests and a Steam-less dry run use.</summary>
    public sealed class SaveStoreTests
    {
        private string _directory;

        [SetUp]
        public void MakeDirectory()
        {
            _directory = Path.Combine(Path.GetTempPath(), "battlebomb-savestore-" + Path.GetRandomFileName());
        }

        [TearDown]
        public void RemoveDirectory()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        [Test]
        public void The_file_store_writes_reads_lists_and_deletes()
        {
            var store = new FileSaveStore(_directory);

            Assert.That(store.Exists("local"), Is.False);
            store.Write("local", "{\"_coins\":1}");

            Assert.That(store.Exists("local"), Is.True);
            Assert.That(store.TryRead("local", out string text), Is.True);
            Assert.That(text, Is.EqualTo("{\"_coins\":1}"));
            Assert.That(store.Names(), Is.EquivalentTo(new[] { "local" }));

            store.Delete("local");
            Assert.That(store.Exists("local"), Is.False);
            Assert.That(store.TryRead("local", out _), Is.False);
        }

        [Test]
        public void A_write_is_atomic_enough_to_leave_no_temp_file_behind()
        {
            var store = new FileSaveStore(_directory);
            store.Write("local", "one");
            store.Write("local", "two");

            Assert.That(Directory.GetFiles(_directory).Length, Is.EqualTo(1), "Only the save itself remains.");
            store.TryRead("local", out string text);
            Assert.That(text, Is.EqualTo("two"));
        }

        [Test]
        public void Names_are_sanitised_so_a_platform_id_cannot_escape_the_folder()
        {
            var store = new FileSaveStore(_directory);
            store.Write("../../escape", "x");

            Assert.That(Directory.GetFiles(_directory).Length, Is.EqualTo(1));
            Assert.That(store.Exists("../../escape"), Is.True, "The same mangling on read finds it again.");
        }

        [Test]
        public void The_memory_store_behaves_the_same_without_a_disk()
        {
            var store = new MemorySaveStore();
            store.Write("a", "1");
            store.Write("b", "2");

            Assert.That(store.TryRead("a", out string text), Is.True);
            Assert.That(text, Is.EqualTo("1"));
            Assert.That(store.Names(), Is.EquivalentTo(new[] { "a", "b" }));
            store.Delete("a");
            Assert.That(store.Exists("a"), Is.False);
        }
    }
}
