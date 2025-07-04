using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Akasha.Modifier
{
    public readonly struct ModifierKey : IEquatable<ModifierKey>, IComparable<ModifierKey>
    {
        private static readonly Dictionary<string, int> StringToId = new();
        private static int NextId = 1;

#if UNITY_EDITOR || DEBUG
        private static readonly Dictionary<int, string> IdToString = new();
#endif

        public readonly int Id;

        private ModifierKey(int id)
        {
            Id = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ModifierKey Create(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Modifier name cannot be null or empty");

            if (!StringToId.TryGetValue(name, out var id))
            {
                id = NextId++;
                StringToId[name] = id;

#if UNITY_EDITOR || DEBUG
                IdToString[id] = name;
#endif
            }

            return new ModifierKey(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ModifierKey Create(Enum enumValue)
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));

            return Create($"{enumValue.GetType().Name}.{enumValue}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ModifierKey other) => Id == other.Id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is ModifierKey other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(ModifierKey other) => Id.CompareTo(other.Id);

        public override string ToString()
        {
#if UNITY_EDITOR || DEBUG
            return IdToString.TryGetValue(Id, out var name) ? name : $"UnknownKey_{Id}";
#else
        return $"Key_{Id}";
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ModifierKey left, ModifierKey right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ModifierKey left, ModifierKey right) => !left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ModifierKey(string name) => Create(name);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ModifierKey(Enum enumValue) => Create(enumValue);

        public static readonly ModifierKey Empty = new(0);

        public bool IsValid => Id > 0;

        public static void ClearCache()
        {
            StringToId.Clear();
            NextId = 1;

#if UNITY_EDITOR || DEBUG
            IdToString.Clear();
#endif
        }

        public static int GetRegisteredCount() => StringToId.Count;

#if UNITY_EDITOR || DEBUG
        public static IEnumerable<(int id, string name)> GetAllRegistered()
        {
            foreach (var kvp in IdToString)
            {
                yield return (kvp.Key, kvp.Value);
            }
        }
#endif
    }
}