using System;
using UnityEngine;

namespace Akasha.State
{
    public static class StateDebugExtensions
    {
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