using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Akasha.Modifier
{
    public enum StackBehavior
    {
        Stack,
        ReplaceLatest,
        KeepFirst,
        TakeMaximum,
        TakeMinimum
    }

    public readonly struct ModifierData : IEquatable<ModifierData>
    {
        public readonly ModifierKey Key;
        public readonly ModifierType Type;
        public readonly float Value;
        public readonly int Order;
        public readonly StackBehavior StackBehavior;
        public readonly int StackId;

        public ModifierData(ModifierKey key, ModifierType type, float value, int order, StackBehavior stackBehavior = StackBehavior.Stack, int stackId = 0)
        {
            Key = key;
            Type = type;
            Value = value;
            Order = order;
            StackBehavior = stackBehavior;
            StackId = stackId;
        }

        public ModifierKey FullKey => StackId == 0 ? Key : ModifierKey.Create($"{Key}[{StackId}]");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ModifierData other) => Key.Equals(other.Key) && StackId == other.StackId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is ModifierData other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(Key, StackId);

        public override string ToString() => StackId == 0
            ? $"{Type}[{Key}] = {Value} (Order: {Order}, Stack: {StackBehavior})"
            : $"{Type}[{Key}[{StackId}]] = {Value} (Order: {Order}, Stack: {StackBehavior})";
    }

    public class ModifierContainer
    {
        private const int InitialCapacity = 8;
        private const float GrowthFactor = 1.5f;

        private ModifierKey[] keys;
        private ModifierType[] types;
        private float[] values;
        private int[] orders;
        private StackBehavior[] stackBehaviors;
        private int[] stackIds;
        private int count;

        private readonly Dictionary<(ModifierKey, int), int> keyToIndex;
        private readonly Dictionary<ModifierType, List<int>> typeIndices;
        private readonly Dictionary<ModifierKey, List<int>> baseKeyIndices;

        public int Count => count;
        public bool IsEmpty => count == 0;

        public ModifierContainer()
        {
            keys = new ModifierKey[InitialCapacity];
            types = new ModifierType[InitialCapacity];
            values = new float[InitialCapacity];
            orders = new int[InitialCapacity];
            stackBehaviors = new StackBehavior[InitialCapacity];
            stackIds = new int[InitialCapacity];

            keyToIndex = new Dictionary<(ModifierKey, int), int>();
            typeIndices = new Dictionary<ModifierType, List<int>>();
            baseKeyIndices = new Dictionary<ModifierKey, List<int>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddModifier(ModifierKey key, ModifierType type, float value, int order, StackBehavior stackBehavior = StackBehavior.Stack, int stackId = 0)
        {
            if (stackBehavior == StackBehavior.Stack)
            {
                AddStackableModifier(key, type, value, order, stackBehavior, stackId);
            }
            else
            {
                AddNonStackableModifier(key, type, value, order, stackBehavior);
            }
        }

        private void AddStackableModifier(ModifierKey key, ModifierType type, float value, int order, StackBehavior stackBehavior, int stackId)
        {
            // 같은 키+스택ID 조합이 이미 존재하는지 확인
            if (keyToIndex.ContainsKey((key, stackId)))
            {
                UnityEngine.Debug.LogWarning($"[ModifierContainer] Modifier with key '{key}' and stackId '{stackId}' already exists. Skipping addition.");
                return;
            }

            EnsureCapacity();

            int index = count;
            keys[index] = key;
            types[index] = type;
            values[index] = value;
            orders[index] = order;
            stackBehaviors[index] = stackBehavior;
            stackIds[index] = stackId;

            keyToIndex[(key, stackId)] = index;
            AddToTypeIndex(type, index);
            AddToBaseKeyIndex(key, index);

            count++;
        }

        private void AddNonStackableModifier(ModifierKey key, ModifierType type, float value, int order, StackBehavior stackBehavior)
        {
            if (baseKeyIndices.TryGetValue(key, out var existingIndices))
            {
                var existingIndex = -1;
                for (int i = 0; i < existingIndices.Count; i++)
                {
                    var idx = existingIndices[i];
                    if (types[idx] == type)
                    {
                        existingIndex = idx;
                        break;
                    }
                }

                if (existingIndex >= 0)
                {
                    switch (stackBehavior)
                    {
                        case StackBehavior.ReplaceLatest:
                            values[existingIndex] = value;
                            orders[existingIndex] = order;
                            break;

                        case StackBehavior.KeepFirst:
                            break;

                        case StackBehavior.TakeMaximum:
                            if (value > values[existingIndex])
                            {
                                values[existingIndex] = value;
                                orders[existingIndex] = order;
                            }
                            break;

                        case StackBehavior.TakeMinimum:
                            if (value < values[existingIndex])
                            {
                                values[existingIndex] = value;
                                orders[existingIndex] = order;
                            }
                            break;
                    }
                    return;
                }
            }

            AddStackableModifier(key, type, value, order, stackBehavior, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveModifier(ModifierKey key, int stackId = 0)
        {
            if (!keyToIndex.TryGetValue((key, stackId), out var removeIndex))
                return false;

            var removedType = types[removeIndex];
            var removedKey = keys[removeIndex];

            var lastIndex = count - 1;

            if (removeIndex != lastIndex)
            {
                keys[removeIndex] = keys[lastIndex];
                types[removeIndex] = types[lastIndex];
                values[removeIndex] = values[lastIndex];
                orders[removeIndex] = orders[lastIndex];
                stackBehaviors[removeIndex] = stackBehaviors[lastIndex];
                stackIds[removeIndex] = stackIds[lastIndex];

                keyToIndex[(keys[removeIndex], stackIds[removeIndex])] = removeIndex;

                UpdateTypeIndices(types[removeIndex], lastIndex, removeIndex);
                UpdateBaseKeyIndices(keys[removeIndex], lastIndex, removeIndex);
            }

            UpdateTypeIndices(removedType, removeIndex, -1);
            UpdateBaseKeyIndices(removedKey, removeIndex, -1);
            keyToIndex.Remove((key, stackId));
            count--;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveByBaseKey(ModifierKey baseKey)
        {
            if (!baseKeyIndices.TryGetValue(baseKey, out var indices))
                return false;

            var keysToRemove = new List<(ModifierKey, int)>();
            foreach (var index in indices)
            {
                keysToRemove.Add((keys[index], stackIds[index]));
            }

            foreach (var (key, stackId) in keysToRemove)
            {
                RemoveModifier(key, stackId);
            }

            return keysToRemove.Count > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveByBaseKeyAndType(ModifierKey baseKey, ModifierType type)
        {
            if (!baseKeyIndices.TryGetValue(baseKey, out var indices))
                return false;

            var keysToRemove = new List<(ModifierKey, int)>();
            foreach (var index in indices)
            {
                if (types[index] == type)
                {
                    keysToRemove.Add((keys[index], stackIds[index]));
                }
            }

            foreach (var (key, stackId) in keysToRemove)
            {
                RemoveModifier(key, stackId);
            }

            return keysToRemove.Count > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToTypeIndex(ModifierType type, int index)
        {
            if (!typeIndices.TryGetValue(type, out var indices))
            {
                indices = new List<int>();
                typeIndices[type] = indices;
            }
            indices.Add(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToBaseKeyIndex(ModifierKey baseKey, int index)
        {
            if (!baseKeyIndices.TryGetValue(baseKey, out var indices))
            {
                indices = new List<int>();
                baseKeyIndices[baseKey] = indices;
            }
            indices.Add(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateTypeIndices(ModifierType type, int oldIndex, int newIndex)
        {
            if (typeIndices.TryGetValue(type, out var indices))
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    if (indices[i] == oldIndex)
                    {
                        if (newIndex >= 0)
                        {
                            indices[i] = newIndex;
                        }
                        else
                        {
                            indices.RemoveAt(i);
                            if (indices.Count == 0)
                            {
                                typeIndices.Remove(type);
                            }
                        }
                        break;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateBaseKeyIndices(ModifierKey baseKey, int oldIndex, int newIndex)
        {
            if (baseKeyIndices.TryGetValue(baseKey, out var indices))
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    if (indices[i] == oldIndex)
                    {
                        if (newIndex >= 0)
                        {
                            indices[i] = newIndex;
                        }
                        else
                        {
                            indices.RemoveAt(i);
                            if (indices.Count == 0)
                            {
                                baseKeyIndices.Remove(baseKey);
                            }
                        }
                        break;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetTypeSum(ModifierType type)
        {
            if (!typeIndices.TryGetValue(type, out var indices))
                return 0f;

            float sum = 0f;
            for (int i = 0; i < indices.Count; i++)
            {
                sum += values[indices[i]];
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetTypeProduct(ModifierType type)
        {
            if (!typeIndices.TryGetValue(type, out var indices))
                return 1f;

            float product = 1f;
            for (int i = 0; i < indices.Count; i++)
            {
                product *= values[indices[i]];
            }
            return product;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(ModifierKey key, int stackId = 0) => keyToIndex.ContainsKey((key, stackId));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsBaseKey(ModifierKey baseKey) => baseKeyIndices.ContainsKey(baseKey);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetModifier(ModifierKey key, out ModifierData data, int stackId = 0)
        {
            if (keyToIndex.TryGetValue((key, stackId), out var index))
            {
                data = new ModifierData(keys[index], types[index], values[index], orders[index], stackBehaviors[index], stackIds[index]);
                return true;
            }
            data = default;
            return false;
        }

        public void Clear()
        {
            count = 0;
            keyToIndex.Clear();
            typeIndices.Clear();
            baseKeyIndices.Clear();
        }

        public int RemoveByPredicate(Func<ModifierData, bool> predicate)
        {
            var toRemove = new List<(ModifierKey, int)>();

            for (int i = 0; i < count; i++)
            {
                var data = new ModifierData(keys[i], types[i], values[i], orders[i], stackBehaviors[i], stackIds[i]);
                if (predicate(data))
                {
                    toRemove.Add((keys[i], stackIds[i]));
                }
            }

            foreach (var (key, stackId) in toRemove)
            {
                RemoveModifier(key, stackId);
            }

            return toRemove.Count;
        }

        public int RemoveByType(ModifierType type)
        {
            if (!typeIndices.TryGetValue(type, out var indices))
                return 0;

            var keysToRemove = new (ModifierKey, int)[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                var index = indices[i];
                keysToRemove[i] = (keys[index], stackIds[index]);
            }

            foreach (var (key, stackId) in keysToRemove)
            {
                RemoveModifier(key, stackId);
            }

            return keysToRemove.Length;
        }

        public Span<ModifierData> GetAllAsSpan()
        {
            var result = new ModifierData[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = new ModifierData(keys[i], types[i], values[i], orders[i], stackBehaviors[i], stackIds[i]);
            }
            return result.AsSpan();
        }

        public IEnumerable<ModifierData> GetByType(ModifierType type)
        {
            if (!typeIndices.TryGetValue(type, out var indices))
                yield break;

            for (int i = 0; i < indices.Count; i++)
            {
                var index = indices[i];
                yield return new ModifierData(keys[index], types[index], values[index], orders[index], stackBehaviors[index], stackIds[index]);
            }
        }

        public IEnumerable<ModifierData> GetByBaseKey(ModifierKey baseKey)
        {
            if (!baseKeyIndices.TryGetValue(baseKey, out var indices))
                yield break;

            for (int i = 0; i < indices.Count; i++)
            {
                var index = indices[i];
                yield return new ModifierData(keys[index], types[index], values[index], orders[index], stackBehaviors[index], stackIds[index]);
            }
        }

        public int GetStackCount(ModifierKey baseKey)
        {
            return baseKeyIndices.TryGetValue(baseKey, out var indices) ? indices.Count : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity()
        {
            if (count >= keys.Length)
            {
                int newCapacity = (int)(keys.Length * GrowthFactor);
                Array.Resize(ref keys, newCapacity);
                Array.Resize(ref types, newCapacity);
                Array.Resize(ref values, newCapacity);
                Array.Resize(ref orders, newCapacity);
                Array.Resize(ref stackBehaviors, newCapacity);
                Array.Resize(ref stackIds, newCapacity);
            }
        }

        public string GetDebugInfo()
        {
            return $"Capacity: {keys.Length}, Count: {count}, Types: {typeIndices.Count}, BaseKeys: {baseKeyIndices.Count}";
        }
    }
}