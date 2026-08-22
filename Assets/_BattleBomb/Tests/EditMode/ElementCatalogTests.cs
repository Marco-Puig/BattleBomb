using System.Collections.Generic;
using BattleBomb.Core.Combat;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D38's promise, pinned: an element is data. Every test here invents its own elements at
    /// runtime — if any of them ever needs a code change to add one, the promise has broken and
    /// the roster (O11) has silently become a code decision again.
    /// </summary>
    public sealed class ElementCatalogTests
    {
        private static ElementCatalog Catalog(params (int Id, string Name)[] rows)
        {
            var specs = new List<ElementSpec>(rows.Length);
            foreach ((int id, string name) in rows)
            {
                specs.Add(new ElementSpec(new ElementId(id), name));
            }

            return new ElementCatalog(specs);
        }

        [Test]
        public void An_element_invented_here_names_itself_through_the_catalog()
        {
            ElementCatalog catalog = Catalog((1, "Fire"), (7, "Poison"));

            Assert.That(catalog.NameOf(new ElementId(1)), Is.EqualTo("Fire"));
            Assert.That(catalog.NameOf(new ElementId(7)), Is.EqualTo("Poison"),
                "An element with any id at all works — the roster is not a contiguous enum.");
            Assert.That(catalog.Count, Is.EqualTo(2));
        }

        [Test]
        public void Ids_come_back_in_authoring_order_for_the_element_roll()
        {
            ElementCatalog catalog = Catalog((3, "Earth"), (1, "Fire"));

            Assert.That(catalog.Ids.Count, Is.EqualTo(2));
            Assert.That(catalog.Ids[0], Is.EqualTo(new ElementId(3)));
            Assert.That(catalog.Ids[1], Is.EqualTo(new ElementId(1)),
                "Order is the authored order, so a generator's draws are reproducible.");
        }

        [Test]
        public void An_unknown_element_falls_back_to_its_id_rather_than_throwing()
        {
            ElementCatalog catalog = Catalog((1, "Fire"));

            Assert.That(catalog.TryGet(new ElementId(2), out _), Is.False);
            Assert.That(catalog.NameOf(new ElementId(2)), Is.EqualTo("Element 2"));
        }

        [Test]
        public void None_is_never_an_element()
        {
            ElementCatalog catalog = Catalog((0, "Nothing"), (1, "Fire"));

            Assert.That(catalog.Count, Is.EqualTo(1), "Id 0 is reserved for kinetic damage.");
            Assert.That(catalog.TryGet(ElementId.None, out _), Is.False);
            Assert.That(ElementId.None.IsNone, Is.True);
        }

        [Test]
        public void A_duplicate_id_is_dropped_and_the_first_authored_one_wins()
        {
            ElementCatalog catalog = Catalog((1, "Fire"), (1, "Flame"));

            Assert.That(catalog.Count, Is.EqualTo(1));
            Assert.That(catalog.NameOf(new ElementId(1)), Is.EqualTo("Fire"));
        }

        [Test]
        public void The_empty_catalog_is_usable_rather_than_null()
        {
            Assert.That(ElementCatalog.Empty.Count, Is.EqualTo(0));
            Assert.That(ElementCatalog.Empty.Ids.Count, Is.EqualTo(0));
            Assert.That(ElementCatalog.Empty.NameOf(new ElementId(1)), Is.EqualTo("Element 1"));
        }

        [Test]
        public void A_negative_id_collapses_to_none()
        {
            Assert.That(new ElementId(-4).Value, Is.EqualTo(0));
            Assert.That(new ElementId(-4), Is.EqualTo(ElementId.None));
        }
    }
}
