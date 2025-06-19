using System;
using System.Collections.Generic;
using System.Linq;

public enum ModifierType
{
    OriginAdd,
    AddMultiplier,
    Multiplier,
    FinalAdd
}

public interface IModifiable
{
    void SetModifier(ModifierType type, ModifierKey key, float value);
    void RemoveModifier(ModifierKey key);
    void RemoveModifier(ModifierType type, ModifierKey key);
    void RemoveModifiersByPredicate(Func<ModifierData, bool> predicate);
}

public readonly struct ModifierData
{
    public readonly ModifierKey Key;
    public readonly ModifierType Type;
    public readonly float Value;
    public readonly DateTime AppliedTime;
    public readonly int Order;

    public ModifierData(ModifierKey key, ModifierType type, float value, int order)
    {
        Key = key;
        Type = type;
        Value = value;
        AppliedTime = DateTime.Now;
        Order = order;
    }

    public override string ToString()
    {
        return $"{Type}[{Key}] = {Value} (Order: {Order})";
    }
}

public class ModifierContainer
{
    private readonly Dictionary<ModifierKey, LinkedListNode<ModifierData>> lookup = new();
    private readonly LinkedList<ModifierData> modifiers = new();
    private readonly Dictionary<ModifierType, HashSet<ModifierKey>> typeIndex = new();
    private int nextOrder = 0;

    public int Count => modifiers.Count;
    public bool IsEmpty => modifiers.Count == 0;

    public void AddModifier(ModifierKey key, ModifierType type, float value)
    {
        RemoveModifier(key);

        var data = new ModifierData(key, type, value, nextOrder++);
        var node = modifiers.AddLast(data);

        lookup[key] = node;

        if (!typeIndex.ContainsKey(type))
            typeIndex[type] = new HashSet<ModifierKey>();
        typeIndex[type].Add(key);
    }

    public bool RemoveModifier(ModifierKey key)
    {
        if (lookup.TryGetValue(key, out var node))
        {
            var data = node.Value;

            modifiers.Remove(node);
            lookup.Remove(key);

            if (typeIndex.TryGetValue(data.Type, out var typeSet))
            {
                typeSet.Remove(key);
                if (typeSet.Count == 0)
                    typeIndex.Remove(data.Type);
            }

            return true;
        }
        return false;
    }

    public bool RemoveModifier(ModifierType type, ModifierKey key)
    {
        if (lookup.TryGetValue(key, out var node) && node.Value.Type == type)
        {
            return RemoveModifier(key);
        }
        return false;
    }

    public int RemoveModifiersByPredicate(Func<ModifierData, bool> predicate)
    {
        var toRemove = new List<ModifierKey>();

        foreach (var modifier in modifiers)
        {
            if (predicate(modifier))
                toRemove.Add(modifier.Key);
        }

        foreach (var key in toRemove)
            RemoveModifier(key);

        return toRemove.Count;
    }

    public int RemoveAllModifiersOfType(ModifierType type)
    {
        if (!typeIndex.TryGetValue(type, out var keys))
            return 0;

        var keysCopy = keys.ToArray();
        foreach (var key in keysCopy)
            RemoveModifier(key);

        return keysCopy.Length;
    }

    public void Clear()
    {
        modifiers.Clear();
        lookup.Clear();
        typeIndex.Clear();
        nextOrder = 0;
    }

    public bool ContainsKey(ModifierKey key) => lookup.ContainsKey(key);

    public bool TryGetModifier(ModifierKey key, out ModifierData data)
    {
        if (lookup.TryGetValue(key, out var node))
        {
            data = node.Value;
            return true;
        }
        data = default;
        return false;
    }

    public IEnumerable<float> GetValuesByType(ModifierType type)
    {
        if (!typeIndex.TryGetValue(type, out var keys))
            yield break;

        foreach (var key in keys)
        {
            if (lookup.TryGetValue(key, out var node))
                yield return node.Value.Value;
        }
    }

    public IEnumerable<ModifierData> GetAllModifiers()
    {
        return modifiers;
    }

    public IEnumerable<ModifierData> GetModifiersByType(ModifierType type)
    {
        return modifiers.Where(m => m.Type == type);
    }
}

public sealed class RxMod<T> : RxBase, IModifiable, IRxField<T>
{
    private T origin;
    private T cachedValue;
    private float debugSum, debugAddMul, debugMul, debugPostAdd;
    private readonly List<Action<T>> listeners = new();
    private readonly ModifierContainer[] containers;
    private readonly ModifierContainer additives = new();
    private readonly ModifierContainer additiveMultipliers = new();
    private readonly ModifierContainer multipliers = new();
    private readonly ModifierContainer postMultiplicativeAdditives = new();

    public string FieldName { get; set; } = string.Empty;
    public T Value => cachedValue;

    public RxMod(T origin = default(T), string fieldName = null, IRxOwner owner = null)
    {
        if (owner != null && !owner.IsRxAllOwner)
            throw new InvalidOperationException($"An invalid owner({owner}) has accessed.");

        this.origin = origin;
        this.cachedValue = origin;

        if (!string.IsNullOrEmpty(fieldName))
            FieldName = fieldName;

        containers = new[] { additives, additiveMultipliers, multipliers, postMultiplicativeAdditives };

        owner?.RegisterRx(this);
        Recalculate();
    }

    public void AddListener(Action<T> listener)
    {
        if (listener != null)
        {
            listeners.Add(listener);
            listener(Value);
        }
    }

    public void RemoveListener(Action<T> listener)
    {
        listeners.Remove(listener);
    }

    private void Recalculate()
    {
        T oldValue = cachedValue;
        CalculateValue();

        if (!AreEqual(oldValue, cachedValue))
        {
            NotifyAll(cachedValue);
        }
    }

    private void CalculateValue()
    {
        float sum = ToFloat(origin);
        foreach (var value in additives.GetValuesByType(ModifierType.OriginAdd))
            sum += value;

        float addMul = 1f;
        foreach (var value in additiveMultipliers.GetValuesByType(ModifierType.AddMultiplier))
            addMul += value;

        float mul = 1f;
        foreach (var value in multipliers.GetValuesByType(ModifierType.Multiplier))
            mul *= value;

        float postAdd = 0f;
        foreach (var value in postMultiplicativeAdditives.GetValuesByType(ModifierType.FinalAdd))
            postAdd += value;

        float result = (sum * addMul * mul) + postAdd;

        debugSum = sum;
        debugAddMul = addMul;
        debugMul = mul;
        debugPostAdd = postAdd;

        cachedValue = FromFloat(result);
    }

    private static float ToFloat(T value)
    {
        return value switch
        {
            int intVal => intVal,
            float floatVal => floatVal,
            long longVal => longVal,
            double doubleVal => (float)doubleVal,
            _ => throw new NotSupportedException($"Type {typeof(T)} is not supported for RxMod")
        };
    }

    private static T FromFloat(float value)
    {
        return typeof(T) switch
        {
            Type t when t == typeof(int) => (T)(object)(int)Math.Round(Math.Clamp(value, int.MinValue, int.MaxValue)),
            Type t when t == typeof(float) => (T)(object)value,
            Type t when t == typeof(long) => (T)(object)(long)Math.Round(Math.Clamp(value, long.MinValue, long.MaxValue)),
            Type t when t == typeof(double) => (T)(object)(double)value,
            _ => throw new NotSupportedException($"Type {typeof(T)} is not supported for RxMod")
        };
    }

    private static bool AreEqual(T a, T b)
    {
        return typeof(T) switch
        {
            Type t when t == typeof(int) => (int)(object)a == (int)(object)b,
            Type t when t == typeof(float) => Math.Abs((float)(object)a - (float)(object)b) < 0.0001f,
            Type t when t == typeof(long) => (long)(object)a == (long)(object)b,
            Type t when t == typeof(double) => Math.Abs((double)(object)a - (double)(object)b) < 0.0001,
            _ => EqualityComparer<T>.Default.Equals(a, b)
        };
    }

    public void ClearAll()
    {
        foreach (var container in containers)
            container.Clear();
        Recalculate();
    }

    public void SetModifier(ModifierType type, ModifierKey key, float value)
    {
        var container = GetContainer(type);
        container.AddModifier(key, type, value);
        Recalculate();
    }

    public void RemoveModifier(ModifierKey key)
    {
        bool removed = false;
        foreach (var container in containers)
            removed |= container.RemoveModifier(key);

        if (removed)
            Recalculate();
    }

    public void RemoveModifier(ModifierType type, ModifierKey key)
    {
        var container = GetContainer(type);
        if (container.RemoveModifier(key))
            Recalculate();
    }

    public void RemoveModifiersByPredicate(Func<ModifierData, bool> predicate)
    {
        int totalRemoved = 0;
        foreach (var container in containers)
            totalRemoved += container.RemoveModifiersByPredicate(predicate);

        if (totalRemoved > 0)
            Recalculate();
    }

    public void RemoveAllModifiersOfType(ModifierType type)
    {
        var container = GetContainer(type);
        if (container.Count > 0)
        {
            container.Clear();
            Recalculate();
        }
    }

    private ModifierContainer GetContainer(ModifierType type)
    {
        return type switch
        {
            ModifierType.OriginAdd => additives,
            ModifierType.AddMultiplier => additiveMultipliers,
            ModifierType.Multiplier => multipliers,
            ModifierType.FinalAdd => postMultiplicativeAdditives,
            _ => throw new ArgumentException($"Unknown modifier type: {type}")
        };
    }

    public void SetValue(T value, IRxCaller caller)
    {
        if (!caller.IsFunctionalCaller)
            throw new InvalidOperationException($"An invalid caller({caller}) has accessed.");
        origin = value;
        Recalculate();
    }

    public void Set(T value)
    {
        origin = value;
        Recalculate();
    }

    public void ResetValue(T newValue)
    {
        origin = newValue;
        ClearAll();
        cachedValue = newValue;
        NotifyAll(cachedValue);
    }

    public override bool Satisfies(Func<object, bool> predicate)
        => predicate?.Invoke(Value) ?? false;

    public override void ClearRelation()
    {
        ClearAll();
        listeners.Clear();
    }

    private void NotifyAll(T value)
    {
        foreach (var l in listeners)
            l(value);
    }

    public IEnumerable<ModifierData> GetAllModifiersInfo()
    {
        foreach (var container in containers)
        {
            foreach (var modifier in container.GetAllModifiers())
                yield return modifier;
        }
    }

    public bool TryGetModifierInfo(ModifierKey key, out ModifierData data)
    {
        foreach (var container in containers)
        {
            if (container.TryGetModifier(key, out data))
                return true;
        }

        data = default;
        return false;
    }

    public string DebugFormula
    {
        get
        {
            return $"({debugSum:F2}) * {debugAddMul:F2} * {debugMul:F2} + {debugPostAdd:F2} = {Value}";
        }
    }

    public string DetailedDebugInfo
    {
        get
        {
            var modifiers = GetAllModifiersInfo().OrderBy(m => m.Order);
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"RxMod<{typeof(T).Name}> '{FieldName}': {Value}");
            sb.AppendLine($"Origin: {origin}");
            sb.AppendLine("Applied Modifiers:");

            foreach (var modifier in modifiers)
            {
                sb.AppendLine($"  {modifier}");
            }

            sb.Append($"Formula: {DebugFormula}");
            return sb.ToString();
        }
    }
}

public readonly struct ModifierKey : IEquatable<ModifierKey>
{
    public readonly Enum Id;

    public ModifierKey(Enum id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    public override string ToString() => $"{Id.GetType().Name}:{Id}";

    public bool Equals(ModifierKey other) => Equals(Id, other.Id);

    public override bool Equals(object obj) => obj is ModifierKey other && Equals(other);

    public override int GetHashCode() => Id.GetHashCode();

    public static implicit operator ModifierKey(Enum value) => new(value);
}