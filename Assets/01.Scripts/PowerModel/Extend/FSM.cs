using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class FSM<TState> : RxBase where TState : Enum
{
    private readonly RxVar<TState> state;
    private readonly Dictionary<TState, Func<TState, bool>> guards = new();
    private readonly Dictionary<TState, Action> onEnter = new();
    private readonly Dictionary<TState, Action> onExit = new();
    private readonly List<Action<TState>> listeners = new();
    private readonly Dictionary<TState, int> priorities = new();

    public FSM(TState initial, IRxOwner owner)
    {
        if(!owner.IsRxAllOwner)
            throw new InvalidOperationException($"An invalid owner({owner}) has accessed.");

        state = new RxVar<TState>(initial, owner);
        state.AddListener(NotifyAll);

        owner.RegisterRx(this);
    }

    public RxVar<TState> State => state;
    public TState Value => state.Value;

    // 상태 변경 직접 요청
    public FSM<TState> Request(TState next)
    {
        if (!CanTransitTo(next)) return this;

        onExit.TryGetValue(Value, out var exit); exit?.Invoke();
        state.Set(next);
        onEnter.TryGetValue(next, out var enter); enter?.Invoke();

        return this;
    }

    public bool CanTransitTo(TState next)
    {
        return !guards.TryGetValue(next, out var cond) || cond(Value);
    }

    public FSM<TState> AddTransitionRule(TState to, Func<TState, bool> rule)
    {
        guards[to] = rule;
        return this;
    }

    public FSM<TState> OnEnter(TState state, Action callback)
    {
        if (!onEnter.ContainsKey(state)) onEnter[state] = callback;
        else onEnter[state] += callback;
        return this;
    }

    public FSM<TState> OnExit(TState state, Action callback)
    {
        if (!onExit.ContainsKey(state)) onExit[state] = callback;
        else onExit[state] += callback;
        return this;
    }

    public FSM<TState> AddListener(Action<TState> listener)
    {
        if (listener == null) return this;

        listeners.Add(listener);
        listener(Value);
        return this;
    }

    public FSM<TState> RemoveListener(Action<TState> listener)
    {
        listeners.Remove(listener);
        return this;
    }

    private void NotifyAll(TState v)
    {
        foreach (var l in listeners)
            l(v);
    }

    public override void ClearRelation()
    {
        listeners.Clear();
        state.ClearRelation();
    }

    // 우선순위 등록
    public FSM<TState> SetPriority(TState state, int priority)
    {
        priorities[state] = priority;
        return this;
    }

    // 우선순위 기반 전이 요청
    public void RequestByPriority(params TState[] candidates)
    {
        TState? best = default;
        int bestPriority = int.MinValue;

        foreach (var state in candidates)
        {
            if (!CanTransitTo(state)) continue;

            int p = priorities.GetValueOrDefault(state, 0);
            if (best == null || p > bestPriority || (p == bestPriority && Equals(state, Value)))
            {
                best = state;
                bestPriority = p;
            }
        }

        if (best != null && !Equals(best, Value))
            Request(best);
    }

    public void RequestByPriority(TState single)
    {
        RequestByPriority(new[] { single });
    }

    // 디버그 출력
    public FSM<TState> WithDebug(string tag = "[FSM]")
    {
        AddListener(state => UnityEngine.Debug.Log($"{tag} → {state}"));
        return this;
    }

    // RxFlagSet으로 상태 평가
    public FSM<TState> DriveByFlags<TFlag>(RxStateFlagSet<TFlag> flags, Func<RxStateFlagSet<TFlag>, TState> evaluator) where TFlag : Enum
    {
        EvaluateFlags(flags, evaluator);

        foreach (var (flag, _) in flags.Snapshot())
            flags.AddListener(flag, _ => EvaluateFlags(flags, evaluator));

        return this;
    }

    private void EvaluateFlags<TFlag>(RxStateFlagSet<TFlag> flags, Func<RxStateFlagSet<TFlag>, TState> evaluator) where TFlag : Enum
    {
        var next = evaluator(flags);
        RequestByPriority(next);
    }
}
#if UNITY_EDITOR
// FSM partial class implementation to add IRxInspectable interface
public partial class FSM<TState> : IRxInspectable where TState : Enum
{

    public void DrawDebugInspector()
    {
        TState currentState = Value;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // FSM 타입 이름으로 헤더 표시
        EditorGUILayout.LabelField($"FSM<{typeof(TState).Name}>", EditorStyles.boldLabel);

        // 현재 상태를 녹색으로 강조 표시
        GUIStyle currentStateStyle = new(EditorStyles.boldLabel)
        {
            normal = { textColor = Color.green }
        };

        EditorGUILayout.LabelField("현재 상태:", currentState.ToString(), currentStateStyle);

        // 가능한 전이 섹션
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("가능한 전이:", EditorStyles.boldLabel);

        // 모든 가능한 전이 나열
        foreach (TState state in Enum.GetValues(typeof(TState)))
        {
            if (EqualityComparer<TState>.Default.Equals(state, currentState))
                continue;

            bool canTransit = CanTransitTo(state);
            GUIStyle stateStyle = new(EditorStyles.label)
            {
                normal = { textColor = canTransit ? Color.cyan : Color.gray }
            };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(state.ToString(), stateStyle, GUILayout.Width(150));

            GUI.enabled = canTransit;
            if (GUILayout.Button("전이", GUILayout.Width(60)))
            {
                Request(state);
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

}
#endif