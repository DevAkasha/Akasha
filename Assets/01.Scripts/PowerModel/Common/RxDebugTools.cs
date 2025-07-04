using System;
using UnityEngine;
using Akasha.Modifier;
using Akasha.State;

namespace Akasha
{
    public static class RxDebugExtensions
    {
        public static RxVar<T> WithDebug<T>(this RxVar<T> rxVar, string label)
        {
            rxVar.AddListener(v => Debug.Log($"[RxVar] {label} = {v}")); // 값 변경을 구독할 수 있음
            return rxVar;
        }

        public static RxMod<T> WithDebug<T>(this RxMod<T> rxMod, string label)
        {
            rxMod.AddListener(v => Debug.Log($"[RxMod] {label} = {v}")); // 값 변경을 구독할 수 있음
            return rxMod;
        }
        public static FSM<T> WithDebug<T>(this FSM<T> fsm, string label = "[FSM]") where T : Enum
        {
            fsm.State.AddListener(state => Debug.Log($"{label} → {state}"));
            return fsm;
        }

        public static RxStateFlagSet<T> WithDebug<T>(this RxStateFlagSet<T> flagSet, string prefix = "[Flags]") where T : Enum
        {
            foreach (var (flag, _) in flagSet.Snapshot())
            {
                flagSet.AddListener(flag, v => Debug.Log($"{prefix} {flag} = {v}"));
            }
            return flagSet;
        }
    }
}