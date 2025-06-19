using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public readonly struct ModifierData : IEquatable<ModifierData>
{
    public readonly ModifierKey Key;
    public readonly ModifierType Type;
    public readonly float Value;
    public readonly int Order;

    public ModifierData(ModifierKey key, ModifierType type, float value, int order)
    {
        Key = key;
        Type = type;
        Value = value;
        Order = order;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ModifierData other) => Key.Equals(other.Key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object obj) => obj is ModifierData other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Key.GetHashCode();

    public override string ToString() => $"{Type}[{Key}] = {Value} (Order: {Order})";
}

public class ModifierContainer
{
    private const int InitialCapacity = 8;
    private const float GrowthFactor = 1.5f;

    private ModifierKey[] keys;
    private ModifierType[] types;
    private float[] values;
    private int[] orders;
    private int count;

    private readonly Dictionary<ModifierKey, int> keyToIndex;
    private readonly Dictionary<ModifierType, List<int>> typeIndices;

    public int Count => count;
    public bool IsEmpty => count == 0;

    public ModifierContainer()
    {
        keys = new ModifierKey[InitialCapacity];
        types = new ModifierType[InitialCapacity];
        values = new float[InitialCapacity];
        orders = new int[InitialCapacity];

        keyToIndex = new Dictionary<ModifierKey, int>();
        typeIndices = new Dictionary<ModifierType, List<int>>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddModifier(ModifierKey key, ModifierType type, float value, int order)
    {
        RemoveModifier(key);

        EnsureCapacity();

        int index = count;
        keys[index] = key;
        types[index] = type;
        values[index] = value;
        orders[index] = order;

        keyToIndex[key] = index;

        if (!typeIndices.TryGetValue(type, out var indices))
        {
            indices = new List<int>();
            typeIndices[type] = indices;
        }
        indices.Add(index);

        count++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveModifier(ModifierKey key)
    {
        if (!keyToIndex.TryGetValue(key, out var removeIndex))
            return false;

        var removedType = types[removeIndex];

        var lastIndex = count - 1;

        if (removeIndex != lastIndex)
        {
            keys[removeIndex] = keys[lastIndex];
            types[removeIndex] = types[lastIndex];
            values[removeIndex] = values[lastIndex];
            orders[removeIndex] = orders[lastIndex];

            keyToIndex[keys[removeIndex]] = removeIndex;

            UpdateTypeIndices(types[removeIndex], lastIndex, removeIndex);
        }

        UpdateTypeIndices(removedType, removeIndex, -1);
        keyToIndex.Remove(key);
        count--;

        return true;
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
    public bool ContainsKey(ModifierKey key) => keyToIndex.ContainsKey(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetModifier(ModifierKey key, out ModifierData data)
    {
        if (keyToIndex.TryGetValue(key, out var index))
        {
            data = new ModifierData(keys[index], types[index], values[index], orders[index]);
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
    }

    public int RemoveByPredicate(Func<ModifierData, bool> predicate)
    {
        var toRemove = new List<ModifierKey>();

        for (int i = 0; i < count; i++)
        {
            var data = new ModifierData(keys[i], types[i], values[i], orders[i]);
            if (predicate(data))
            {
                toRemove.Add(keys[i]);
            }
        }

        foreach (var key in toRemove)
        {
            RemoveModifier(key);
        }

        return toRemove.Count;
    }

    public int RemoveByType(ModifierType type)
    {
        if (!typeIndices.TryGetValue(type, out var indices))
            return 0;

        var keysToRemove = new ModifierKey[indices.Count];
        for (int i = 0; i < indices.Count; i++)
        {
            keysToRemove[i] = keys[indices[i]];
        }

        foreach (var key in keysToRemove)
        {
            RemoveModifier(key);
        }

        return keysToRemove.Length;
    }

    public Span<ModifierData> GetAllAsSpan()
    {
        var result = new ModifierData[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = new ModifierData(keys[i], types[i], values[i], orders[i]);
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
            yield return new ModifierData(keys[index], types[index], values[index], orders[index]);
        }
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
        }
    }

    public string GetDebugInfo()
    {
        return $"Capacity: {keys.Length}, Count: {count}, Types: {typeIndices.Count}";
    }
}