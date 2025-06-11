using System;
using System.Collections.Generic;
using static UnityEngine.UI.GridLayoutGroup;


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
    void ApplyModifier(ModifierKey key, string fieldName, ModifierType type, object value); // Modifier를 적용
    void ApplySignFlip(ModifierKey key);
    void RemoveModifier(ModifierKey key);
}

public interface IRxModBase
{
    object Value { get; }
    void SetValue(object origin, IRxCaller caller); // 값 설정
    void ResetValue(object origin); // 초기 원본 값
    void SetModifier(ModifierType type, ModifierKey key, object value);
    void AddModifier(ModifierType type, ModifierKey key);
    void RemoveModifier(ModifierType type, ModifierKey key);
    void ClearAll(); // 모든 Modifier 초기화
}

public interface IRxMod<T> : IRxModBase, IRxField<T>
{
    new T Value { get; }
    void SetValue(T origin, IRxCaller caller); // 값 설정
    void ResetValue(T origin); // 초기 원본 값
    void SetModifier(ModifierType type, ModifierKey key, T value);
}

public interface IRxModFormulaProvider
{
    string BuildDebugFormula();
}

public abstract class RxModBase<T> : RxBase, IRxMod<T>, IModifiable, IRxField<T>, IRxModFormulaProvider
{
    protected T origin; // 초기 원본 값
    protected T cachedValue; // 계산된 값 캐싱
    protected T lastNotifiedValue;

    protected readonly List<Action<T>> listeners = new();

    public string FieldName { get; set; } = string.Empty;

    public T Value => cachedValue;

    object IRxModBase.Value => Value;

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

    public void ForceUpdate() => Recalculate(); // 재계산 요청

    protected void Recalculate()
    {
        T oldValue = cachedValue;

        // 하위 클래스에서 구현하는 실제 계산
        CalculateValue();

        // 값이 변경된 경우에만 알림 (타입별 구현 사용)
        if (!AreValuesEqual(oldValue, cachedValue))
        {
            NotifyAll(cachedValue);
        }
    }

    protected abstract bool AreValuesEqual(T a, T b);

    public abstract void SetModifier(ModifierType type, ModifierKey key, T value);

    public void SetValue(T value, IRxCaller caller) // 값 설정
    {
        if(!caller.IsFunctionalCaller)
            throw new InvalidOperationException($"An invalid caller({caller}) has accessed.");
        origin = value; // 초기 원본 값
        ForceUpdate();
    }
    public void Set(T value) // 값 설정
    {
        origin = value; // 초기 원본 값
        ForceUpdate();
    }

    // 하위 클래스에서 구현할 실제 계산 로직
    protected abstract void CalculateValue();

    protected void NotifyAll(T value)
    {
        foreach (var l in listeners)
            l(value);
    }

    void IRxModBase.SetValue(object origin, IRxCaller caller) // 값 설정
    {
        if(!caller.IsFunctionalCaller)
            throw new InvalidOperationException($"An invalid caller({caller}) has accessed.");
        if (origin is T val) Set(val); // 값 설정
        else throw new InvalidCastException($"Expected {typeof(T).Name}");
    }

    public void ResetValue(T newValue)
    {
        origin = newValue; // 초기 원본 값
        ClearAll(); // 모든 Modifier 초기화
        cachedValue = newValue; // 계산된 값 캐싱
        lastNotifiedValue = newValue;
    }

    void IRxModBase.ResetValue(object origin) // 초기 원본 값
    {
        if (origin is T val) ResetValue(val); // 초기 원본 값
        else throw new InvalidCastException($"Expected {typeof(T).Name}");
    }

    public override bool Satisfies(Func<object, bool> predicate)
        => predicate?.Invoke(Value) ?? false;

    public override void ClearRelation()
    {
        listeners.Clear();
        ClearAll(); // 모든 Modifier 초기화
    }

    public abstract void ClearAll(); // 모든 Modifier 초기화


    void IRxModBase.SetModifier(ModifierType type, ModifierKey key, object value)
    {
        if (value is T typed) SetModifier(type, key, typed);
        else throw new InvalidCastException($"Expected {typeof(T).Name}");
    }

    public abstract void AddModifier(ModifierType type, ModifierKey key);
    public abstract void RemoveModifier(ModifierType type, ModifierKey key);

    void IRxModBase.AddModifier(ModifierType type, ModifierKey key)
    {
        AddModifier(type, key);
    }

    void IRxModBase.RemoveModifier(ModifierType type, ModifierKey key)
    {
        RemoveModifier(type, key);
    }

    void IRxModBase.ClearAll() => ClearAll(); // 모든 Modifier 초기화

    public void ApplyModifier(ModifierKey key, string fieldName, ModifierType type, object value) // Modifier를 적용
    {
        if (!string.Equals(FieldName, fieldName, StringComparison.OrdinalIgnoreCase)) return;
        ((IRxModBase)this).SetModifier(type, key, value);
    }

    public void ApplySignFlip(ModifierKey key) => AddModifier(ModifierType.SignFlip, key);

    public void RemoveModifier(ModifierKey key)
    {
        RemoveModifier(ModifierType.OriginAdd, key);
        RemoveModifier(ModifierType.AddMultiplier, key);
        RemoveModifier(ModifierType.Multiplier, key);
        RemoveModifier(ModifierType.FinalAdd, key);
        RemoveModifier(ModifierType.SignFlip, key);
    }

    public string DebugFormula => BuildDebugFormula();

    protected abstract string BuildDebugFormula();

    string IRxModFormulaProvider.BuildDebugFormula()
    {
        return BuildDebugFormula();
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