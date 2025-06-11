using System;
using UnityEngine;

public static class RxDebugExtensions
{
    public static RxVar<T> WithDebug<T>(this RxVar<T> rxVar, string label)
    {
        rxVar.AddListener(v => Debug.Log($"[RxVar] {label} = {v}")); // 값 변경을 구독할 수 있음
        return rxVar;
    }

    public static IRxMod<T> WithDebug<T>(this IRxMod<T> rxMod, string label)
    {
        rxMod.AddListener(v => Debug.Log($"[RxMod] {label} = {v}")); // 값 변경을 구독할 수 있음
        return rxMod;
    }

    public static FSM<T> WithDebug<T>(this FSM<T> fsm, string label = "[FSM]") where T : Enum
    {
        fsm.State.AddListener(state => Debug.Log($"{label} → {state}")); // 값 변경을 구독할 수 있음
        return fsm;
    }

    public static RxStateFlagSet<T> WithDebug<T>(this RxStateFlagSet<T> flagSet, string prefix = "[Flags]") where T : Enum
    {
        foreach (var (flag, _) in flagSet.Snapshot()) // 현재 상태 스냅샷
        {
            flagSet.AddListener(flag, v => Debug.Log($"{prefix} {flag} = {v}")); // 값 변경을 구독할 수 있음
        }
        return flagSet;
    }
}