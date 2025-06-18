using System;
using System.Collections.Generic;

public enum ModifierType
{
    OriginAdd,
    AddMultiplier,
    Multiplier,
    FinalAdd,
    SignFlip
}

public interface IModifiable
{
    void SetModifier(ModifierType type, ModifierKey key, float value);
    void ApplySignFlip(ModifierKey key);
    void RemoveModifier(ModifierKey key);
}

public sealed class RxMod<T> : RxBase, IModifiable, IRxField<T>
{
    private T origin; // 초기 원본 값
    private T cachedValue; // 계산된 값 캐싱

    private float debugSum, debugAddMul, debugMul, debugPostAdd;
    private bool debugIsNegative;

    private readonly List<Action<T>> listeners = new();

    private readonly Dictionary<ModifierKey, float> additives = new();
    private readonly Dictionary<ModifierKey, float> additiveMultipliers = new();
    private readonly Dictionary<ModifierKey, float> multipliers = new();
    private readonly Dictionary<ModifierKey, float> postMultiplicativeAdditives = new();
    private readonly HashSet<ModifierKey> signModifiers = new();

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

        owner?.RegisterRx(this);
        Recalculate();
    }

    public void AddListener(Action<T> listener) // 값 변경을 구독할 수 있음
    {
        if (listener != null)
        {
            listeners.Add(listener);
            listener(Value);
        }
    }

    public void RemoveListener(Action<T> listener) // 구독 해제
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
        foreach (var v in additives.Values) sum += v;

        float addMul = 1f;
        foreach (var v in additiveMultipliers.Values) addMul += v;

        float mul = 1f;
        foreach (var v in multipliers.Values) mul *= v;

        float postAdd = 0f;
        foreach (var v in postMultiplicativeAdditives.Values) postAdd += v;

        bool isNegative = signModifiers.Count % 2 == 1;

        float result = (sum * addMul * mul) + postAdd;
        float finalResult = isNegative ? -result : result;

        debugSum = sum;
        debugAddMul = addMul;
        debugMul = mul;
        debugPostAdd = postAdd;
        debugIsNegative = isNegative;

        cachedValue = FromFloat(finalResult);
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
        additives.Clear();
        additiveMultipliers.Clear();
        multipliers.Clear();
        postMultiplicativeAdditives.Clear();
        signModifiers.Clear();
        Recalculate();
    }

    public void SetModifier(ModifierType type, ModifierKey key, float value)
    {
        switch (type)
        {
            case ModifierType.OriginAdd: additives[key] = value; break;
            case ModifierType.AddMultiplier: additiveMultipliers[key] = value; break;
            case ModifierType.Multiplier: multipliers[key] = value; break;
            case ModifierType.FinalAdd: postMultiplicativeAdditives[key] = value; break;
            default: throw new InvalidOperationException("Use AddModifier for SignFlip.");
        }
        Recalculate();
    }

    public void AddModifier(ModifierType type, ModifierKey key)
    {
        if (type != ModifierType.SignFlip)
            throw new InvalidOperationException("Only SignFlip can be added without a value.");
        signModifiers.Add(key);
        Recalculate();
    }

    public void RemoveModifier(ModifierType type, ModifierKey key)
    {
        bool removed = type switch
        {
            ModifierType.OriginAdd => additives.Remove(key),
            ModifierType.AddMultiplier => additiveMultipliers.Remove(key),
            ModifierType.Multiplier => multipliers.Remove(key),
            ModifierType.FinalAdd => postMultiplicativeAdditives.Remove(key),
            ModifierType.SignFlip => signModifiers.Remove(key),
            _ => false
        };
        if (removed) Recalculate();
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
  
    public void ApplySignFlip(ModifierKey key) => AddModifier(ModifierType.SignFlip, key);

    public void RemoveModifier(ModifierKey key)
    {
        bool removed = false;
        removed |= additives.Remove(key);
        removed |= additiveMultipliers.Remove(key);
        removed |= multipliers.Remove(key);
        removed |= postMultiplicativeAdditives.Remove(key);
        removed |= signModifiers.Remove(key);

        if (removed)
            Recalculate();
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

    public string DebugFormula
    {
        get
        {
            var sb = new System.Text.StringBuilder();

            if (debugIsNegative)
                sb.Append("-1 * ");

            sb.Append('(').Append(debugSum.ToString("F2")).Append(')');
            sb.Append(" * ").Append(debugAddMul.ToString("F2"));
            sb.Append(" * ").Append(debugMul.ToString("F2"));
            sb.Append(" + ").Append(debugPostAdd.ToString("F2"));
            sb.Append(" = ").Append(Value);

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

    public override string ToString() => $"{Id.GetType().Name}:{Id}"; // 문자열로 요약

    public bool Equals(ModifierKey other) => Equals(Id, other.Id);

    public override bool Equals(object obj) => obj is ModifierKey other && Equals(other);

    public override int GetHashCode() => Id.GetHashCode();

    public static implicit operator ModifierKey(Enum value) => new(value);
}