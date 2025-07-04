using System;
using UnityEngine;
using Akasha.Modifier;

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
    }
}