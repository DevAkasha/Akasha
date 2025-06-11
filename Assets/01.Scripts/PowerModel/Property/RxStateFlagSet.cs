using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

public sealed class RxStateFlag: RxBase // 단일 상태 플래그를 나타내는 클래스
{
    private readonly RxVar<bool> internalFlag; // 내부 상태 값 (true/false)를 저장

#nullable enable
    private Func<bool>? condition; // 조건 기반으로 자동 평가될 수 있는 함수
#nullable disable
    public string Name { get; } // 플래그의 이름 (식별용)
    public bool Value => internalFlag.Value; // 현재 플래그 값 (true/false)

#nullable enable
    public event Action<bool>? OnChanged; // 값이 변경되었을 때 알림
#nullable disable
    internal RxStateFlag(string name, IRxOwner owner) // 조건 기반으로 자동 평가될 수 있는 함수
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        internalFlag = new RxVar<bool>(false, owner); // 내부 상태 값 (true/false)를 저장
        internalFlag.AddListener(HandleChange); // 외부에서 변경 알림을 구독할 수 있음

        if (owner is IRxOwner model)
            model.RegisterRx(this);
    }

    private void HandleChange(bool value)
    {
        OnChanged?.Invoke(value);
    }

    internal void Set(bool value) // 외부에서 값을 설정 (내부용)
    {
        if (condition != null)
            throw new InvalidOperationException($"[RxStateFlag:{Name}] is condition-based.");
        internalFlag.Set(value);
    }

    internal void Evaluate() // condition이 있을 경우 조건을 평가하여 값 갱신
    {
        if (condition != null)
        {
            internalFlag.Set(condition.Invoke());
        }
    }

    internal void SetCondition(Func<bool> newCondition)
    {
        condition = newCondition;
    }

    public void AddListener(Action<bool> listener) // 외부에서 변경 알림을 구독할 수 있음
    {
        internalFlag.AddListener(listener); // 외부에서 변경 알림을 구독할 수 있음
    }

    public void RemoveListener(Action<bool> listener)
    {
        internalFlag.RemoveListener(listener);
    }

    public override string ToString()
    {
        return $"[RxStateFlag] {Name} = {Value}";
    }

    public override void ClearRelation()
    {
        internalFlag.ClearRelation();
        OnChanged = null;
    }
}

public partial class RxStateFlagSet<TEnum> : RxBase where TEnum : Enum // 여러 플래그를 Enum 기반으로 관리하는 클래스
{
    private readonly List<RxStateFlag> flags;
    private readonly Dictionary<TEnum, int> indexMap; // Enum 값을 인덱스로 매핑

    public RxStateFlagSet(IRxOwner owner)
    {
        if(!owner.IsRxAllOwner)
            throw new InvalidOperationException($"An invalid owner({owner}) has accessed.");

        var values = (TEnum[])Enum.GetValues(typeof(TEnum));
        flags = new List<RxStateFlag>(values.Length); // 모든 플래그를 저장하는 리스트
        indexMap = new Dictionary<TEnum, int>(); // Enum 값을 인덱스로 매핑

        for (int i = 0; i < values.Length; i++)
        {
            var enumValue = values[i];
            indexMap[enumValue] = i; // Enum 값을 인덱스로 매핑
            flags.Add(new RxStateFlag(enumValue.ToString(), owner));
        }

        owner.RegisterRx(this);

    }

    public RxStateFlag this[TEnum state] => flags[indexMap[state]]; // Enum 값을 인덱스로 매핑

    public void SetValue(TEnum state, bool value) => this[state].Set(value);

    public bool GetValue(TEnum state) => this[state].Value;
    public void Evaluate(TEnum state) => this[state].Evaluate();

    public void EvaluateAll() // 모든 조건 기반 플래그를 평가
    {
        foreach (var flag in flags)
            flag.Evaluate();
    }

    public void SetCondition(TEnum state, Func<bool> condition) => this[state].SetCondition(condition);
  
    public void AddListener(TEnum state, Action<bool> listener) => this[state].AddListener(listener); // 외부에서 변경 알림을 구독할 수 있음

    public void RemoveListener(TEnum state, Action<bool> listener) => this[state].RemoveListener(listener);

    public bool AnyActive() => flags.Exists(f => f.Value);
    public bool AnyActive(params TEnum[] subset)
    {
        foreach (var flag in subset)
        {
            if (this[flag].Value)
                return true;
        }
        return false;
    }
    public bool AllSatisfied() => flags.TrueForAll(f => f.Value);
    public bool AllSatisfied(params TEnum[] subset)
    {
        foreach (var flag in subset)
        {
            if (!this[flag].Value)
                return false;
        }
        return true;
    }
    public bool NoneActive() => flags.TrueForAll(f => !f.Value);
    public bool NoneActive(params TEnum[] subset)
    {
        foreach (var flag in subset)
        {
            if (this[flag].Value)
                return false;
        }
        return true;
    }
    public IEnumerable<(TEnum, bool)> Snapshot() // 현재 모든 플래그 상태를 (이름, 값) 튜플로 반환
    {
        foreach (var pair in indexMap) // Enum 값을 인덱스로 매핑
        {
            yield return (pair.Key, flags[pair.Value].Value);
        }
    }

    public IEnumerable<TEnum> ActiveFlags() // 현재 true 상태인 플래그만 반환
    {
        foreach (var (key, value) in Snapshot()) // 현재 모든 플래그 상태를 (이름, 값) 튜플로 반환
        {
            if (value) yield return key;
        }
    }
    public override void ClearRelation()
    {
        foreach (var flag in flags)
            flag.ClearRelation();
    }

    public override string ToString()
    {
        return $"RxStateFlagSet<{typeof(TEnum).Name}>: " + string.Join(", ", Snapshot()); // 현재 모든 플래그 상태를 (이름, 값) 튜플로 반환
    }
}

#if UNITY_EDITOR
public interface IRxInspectable
{
    void DrawDebugInspector();
}
public partial class RxStateFlagSet<TEnum> : IRxInspectable where TEnum : Enum
{
    public void DrawDebugInspector()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"RxStateFlagSet<{typeof(TEnum).Name}>", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        foreach (var (key, value) in Snapshot())
        {
            GUIStyle style = new(EditorStyles.label)
            {
                normal = { textColor = value ? Color.green : Color.gray }
            };
            EditorGUILayout.LabelField(key.ToString(), value.ToString(), style);
        }
        EditorGUI.indentLevel--;
    }
}
#endif