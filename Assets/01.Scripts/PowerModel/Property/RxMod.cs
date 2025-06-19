using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public interface IModifiable
{
    void SetModifier(ModifierType type, ModifierKey key, float value);
    void SetModifier(ModifierType type, ModifierKey key, float value, StackBehavior stackBehavior);
    void SetStackModifier(ModifierType type, ModifierKey key, float value, int stackId = -1);
    void RemoveModifier(ModifierKey key);
    void RemoveModifier(ModifierKey key, int stackId);
    void RemoveModifier(ModifierType type, ModifierKey key);
    void RemoveModifiersByPredicate(Func<ModifierData, bool> predicate);
    int GetStackCount(ModifierKey baseKey);
    bool HasStack(ModifierKey baseKey);
    IEnumerable<ModifierData> GetModifiersByBaseKey(ModifierKey baseKey);
}

public sealed class RxMod<T> : RxBase, IModifiable, IRxField<T>
{
    private T origin;
    private T cachedValue;
    private readonly List<Action<T>> listeners;
    private readonly int instanceId;
    private readonly TypeCalculator<T> calculator;

    public string FieldName { get; set; }
    public T Value => cachedValue;

    private static ModifierManager ModifierManager => GameManager.Instance?.GetManager<ModifierManager>();

    public RxMod(T origin = default, string fieldName = null, IRxOwner owner = null)
    {
        if (owner != null && !owner.IsRxAllOwner)
            throw new InvalidOperationException($"An invalid owner({owner}) has accessed.");

        this.origin = origin;
        this.cachedValue = origin;
        this.listeners = new List<Action<T>>();
        this.calculator = TypeCalculator<T>.Instance;

        if (!string.IsNullOrEmpty(fieldName))
            FieldName = fieldName;

        if (ModifierManager != null)
        {
            instanceId = ModifierManager.RegisterInstance();
        }
        else
        {
            instanceId = GetHashCode();
            UnityEngine.Debug.LogWarning("[RxMod] ModifierManager not found - using fallback instance ID");
        }

        owner?.RegisterRx(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddListener(Action<T> listener)
    {
        if (listener != null)
        {
            listeners.Add(listener);
            listener(Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveListener(Action<T> listener)
    {
        listeners.Remove(listener);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetModifier(ModifierType type, ModifierKey key, float value)
    {
        ModifierManager?.SetModifier(instanceId, type, key, value);
        Recalculate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetModifier(ModifierType type, ModifierKey key, float value, StackBehavior stackBehavior)
    {
        ModifierManager?.SetModifier(instanceId, type, key, value, stackBehavior);
        Recalculate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStackModifier(ModifierType type, ModifierKey key, float value, int stackId = -1)
    {
        ModifierManager?.SetStackModifier(instanceId, type, key, value, stackId);
        Recalculate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveModifier(ModifierKey key)
    {
        if (ModifierManager?.RemoveByBaseKey(instanceId, key) == true)
            Recalculate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveModifier(ModifierKey key, int stackId)
    {
        if (ModifierManager?.RemoveModifier(instanceId, key, stackId) == true)
            Recalculate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveModifier(ModifierType type, ModifierKey key)
    {
        if (ModifierManager?.RemoveModifier(instanceId, type, key) == true)
            Recalculate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveModifiersByPredicate(Func<ModifierData, bool> predicate)
    {
        if (ModifierManager?.RemoveModifiersByPredicate(instanceId, predicate) > 0)
            Recalculate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAllModifiersOfType(ModifierType type)
    {
        if (ModifierManager?.RemoveAllModifiersOfType(instanceId, type) > 0)
            Recalculate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearAll()
    {
        ModifierManager?.Clear(instanceId);
        Recalculate();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(T value)
    {
        if (!calculator.AreEqual(origin, value))
        {
            origin = value;
            Recalculate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue(T value, IRxCaller caller)
    {
        if (!caller.IsFunctionalCaller)
            throw new InvalidOperationException($"An invalid caller({caller}) has accessed.");

        Set(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetValue(T newValue)
    {
        origin = newValue;
        ModifierManager?.Clear(instanceId);
        cachedValue = newValue;
        NotifyAll(cachedValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Recalculate()
    {
        T oldValue = cachedValue;

        float baseValue = calculator.ToFloat(origin);
        float calculatedValue = ModifierManager?.CalculateValue(instanceId, baseValue) ?? baseValue;
        cachedValue = calculator.FromFloat(calculatedValue);

        if (!calculator.AreEqual(oldValue, cachedValue))
        {
            NotifyAll(cachedValue);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NotifyAll(T value)
    {
        for (int i = 0; i < listeners.Count; i++)
        {
            listeners[i](value);
        }
    }

    public bool TryGetModifierInfo(ModifierKey key, out ModifierData data, int stackId = 0)
    {
        if (ModifierManager != null)
            return ModifierManager.TryGetModifier(instanceId, key, out data, stackId);

        data = default;
        return false;
    }

    public Span<ModifierData> GetAllModifiersInfo()
    {
        if (ModifierManager == null)
            return Span<ModifierData>.Empty;

        return ModifierManager.GetAllModifiers(instanceId);
    }

    public IEnumerable<ModifierData> GetModifiersByBaseKey(ModifierKey baseKey)
    {
        if (ModifierManager == null)
            return Array.Empty<ModifierData>();

        return ModifierManager.GetModifiersByBaseKey(instanceId, baseKey);
    }

    public int GetStackCount(ModifierKey baseKey)
    {
        if (ModifierManager == null)
            return 0;

        return ModifierManager.GetStackCount(instanceId, baseKey);
    }

    public bool HasStack(ModifierKey baseKey)
    {
        return GetStackCount(baseKey) > 0;
    }

    public bool HasMultipleStacks(ModifierKey baseKey)
    {
        return GetStackCount(baseKey) > 1;
    }

    public string DebugFormula
    {
        get
        {
            float baseValue = calculator.ToFloat(origin);
            return ModifierManager?.GetDebugFormula(instanceId, baseValue) ?? $"{baseValue:F2}";
        }
    }

    public string DetailedDebugInfo
    {
        get
        {
            float baseValue = calculator.ToFloat(origin);
            return ModifierManager?.GetDetailedDebugInfo(instanceId, FieldName ?? "Unknown", baseValue)
                   ?? $"RxMod<{typeof(T).Name}> '{FieldName}': {Value} (No ModifierManager)";
        }
    }

    public override bool Satisfies(Func<object, bool> predicate)
        => predicate?.Invoke(Value) ?? false;

    public override void ClearRelation()
    {
        ModifierManager?.UnregisterInstance(instanceId);
        listeners.Clear();
    }
}

public abstract class TypeCalculator<T>
{
    public static readonly TypeCalculator<T> Instance = CreateInstance();

    private static TypeCalculator<T> CreateInstance()
    {
        return typeof(T) switch
        {
            Type t when t == typeof(int) => (TypeCalculator<T>)(object)new IntCalculator(),
            Type t when t == typeof(float) => (TypeCalculator<T>)(object)new FloatCalculator(),
            Type t when t == typeof(long) => (TypeCalculator<T>)(object)new LongCalculator(),
            Type t when t == typeof(double) => (TypeCalculator<T>)(object)new DoubleCalculator(),
            _ => throw new NotSupportedException($"Type {typeof(T)} is not supported for RxMod")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract float ToFloat(T value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract T FromFloat(float value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract bool AreEqual(T a, T b);
}

public class IntCalculator : TypeCalculator<int>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float ToFloat(int value) => value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int FromFloat(float value) => (int)Math.Round(Math.Clamp(value, int.MinValue, int.MaxValue));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool AreEqual(int a, int b) => a == b;
}

public class FloatCalculator : TypeCalculator<float>
{
    private const float Epsilon = 0.0001f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float ToFloat(float value) => value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float FromFloat(float value) => value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool AreEqual(float a, float b) => Math.Abs(a - b) < Epsilon;
}

public class LongCalculator : TypeCalculator<long>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float ToFloat(long value) => value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long FromFloat(float value) => (long)Math.Round(Math.Clamp(value, long.MinValue, long.MaxValue));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool AreEqual(long a, long b) => a == b;
}

public class DoubleCalculator : TypeCalculator<double>
{
    private const double Epsilon = 0.0001;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float ToFloat(double value) => (float)value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override double FromFloat(float value) => value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool AreEqual(double a, double b) => Math.Abs(a - b) < Epsilon;
}