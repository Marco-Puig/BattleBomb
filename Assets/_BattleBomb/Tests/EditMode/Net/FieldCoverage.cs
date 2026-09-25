using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>
    /// Fills every field of a struct — nested structs and private fields included — with a
    /// distinctive, valid, non-default value by reflection, going around every constructor; and
    /// compares two values field by field. A codec that forgets a field hands back its default, and
    /// <see cref="AssertSame"/> names the path (HANDOFF-M8 planning decision 2). Across seeds 1–3 every
    /// bool is true in one and false in another, no two bools agree in all three, and two enums of one
    /// type never agree, so a codec that swaps two same-typed fields fails as surely as one that drops one.
    /// </summary>
    internal static class FieldCoverage
    {
        private const BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Distinct three-bit codes, never all off or all on: bool k reads bit (seed - 1) % 3 of code k.</summary>
        private const int BoolCodes = 6;

        internal static T Filled<T>(int seed) where T : struct =>
            (T)Fill(typeof(T), new Filler(seed), typeof(T).Name);

        internal static void AssertSame(object expected, object actual, string path)
        {
            string difference = FirstDifference(expected, actual, path);
            Assert.That(difference, Is.Null, $"{difference} did not survive the wire.");
        }

        /// <summary>The path of the first field that differs, or null when every field matches. A plain
        /// value rather than an assertion, so the helper can be tested against a broken codec.</summary>
        internal static string FirstDifference(object expected, object actual, string path)
        {
            if (expected == null || actual == null)
            {
                return Equals(expected, actual) ? null : path;
            }

            Type type = expected.GetType();
            if (type.IsArray)
            {
                var a = (Array)expected;
                var b = (Array)actual;
                if (a.Length != b.Length)
                {
                    return $"{path}.Length";
                }

                for (int i = 0; i < a.Length; i++)
                {
                    string inner = FirstDifference(a.GetValue(i), b.GetValue(i), $"{path}[{i}]");
                    if (inner != null)
                    {
                        return inner;
                    }
                }

                return null;
            }

            if (type.IsPrimitive || type.IsEnum || type == typeof(Vector3) || type == typeof(Vector2) || type == typeof(string))
            {
                return expected.Equals(actual) ? null : path;
            }

            foreach (FieldInfo field in type.GetFields(AllInstance))
            {
                string inner = FirstDifference(field.GetValue(expected), field.GetValue(actual), $"{path}.{field.Name}");
                if (inner != null)
                {
                    return inner;
                }
            }

            return null;
        }

        private static object Fill(Type type, Filler filler, string path)
        {
            if (type == typeof(int))
            {
                return filler.Random.Next(1, 1000);
            }

            if (type == typeof(uint))
            {
                return (uint)filler.Random.Next(1, 1000);
            }

            // Inside (0, 1): every float the wire carries is valid there, including the clamped ones.
            if (type == typeof(float))
            {
                return (float)(0.01 + filler.Random.NextDouble() * 0.98);
            }

            if (type == typeof(bool))
            {
                if (filler.Bools == BoolCodes)
                {
                    throw new NotSupportedException($"{path}: more than {BoolCodes} bools — three seeds can no longer tell them apart.");
                }

                int code = ++filler.Bools;
                return ((code >> filler.Bit) & 1) == 1;
            }

            if (type == typeof(Vector3))
            {
                return new Vector3((float)Fill(typeof(float), filler, path), (float)Fill(typeof(float), filler, path),
                    (float)Fill(typeof(float), filler, path));
            }

            if (type == typeof(Vector2))
            {
                return new Vector2((float)Fill(typeof(float), filler, path), (float)Fill(typeof(float), filler, path));
            }

            if (type.IsEnum)
            {
                // Never the zero default, so a dropped enum field is caught like any other; and turn by
                // turn through the rest, so two fields of one enum type never hold the same value.
                var named = new System.Collections.Generic.List<object>();
                foreach (object value in Enum.GetValues(type))
                {
                    if (Convert.ToInt64(value) != 0)
                    {
                        named.Add(value);
                    }
                }

                if (named.Count == 0)
                {
                    throw new NotSupportedException($"{path}: {type.Name} has no value but its default.");
                }

                filler.Enums.TryGetValue(type, out int turn);
                filler.Enums[type] = turn + 1;
                return named[(turn + filler.Seed) % named.Count];
            }

            if (type.IsArray)
            {
                Type element = type.GetElementType();
                Array array = Array.CreateInstance(element, 2);
                for (int i = 0; i < array.Length; i++)
                {
                    array.SetValue(Fill(element, filler, $"{path}[{i}]"), i);
                }

                return array;
            }

            if (type.IsValueType && !type.IsPrimitive)
            {
                object boxed = Activator.CreateInstance(type);
                foreach (FieldInfo field in type.GetFields(AllInstance))
                {
                    field.SetValue(boxed, Fill(field.FieldType, filler, $"{path}.{field.Name}"));
                }

                return boxed;
            }

            throw new NotSupportedException($"{path}: FieldCoverage cannot fill a {type.Name}.");
        }

        private sealed class Filler
        {
            internal readonly System.Random Random;
            internal readonly int Seed;
            internal readonly int Bit;
            internal readonly System.Collections.Generic.Dictionary<Type, int> Enums =
                new System.Collections.Generic.Dictionary<Type, int>();

            internal int Bools;

            internal Filler(int seed)
            {
                Random = new System.Random(seed);
                Seed = seed;
                Bit = ((seed - 1) % 3 + 3) % 3;
            }
        }
    }
}
