using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EffectManager : ManagerBase
{
    public override int InitializationPriority => 10; // 높은 우선도 (핵심 시스템)

    private readonly Dictionary<ModifierKey, BaseEffect> effects = new();

    #region Manager Lifecycle
    protected override void OnManagerAwake()
    {
        base.OnManagerAwake();
        RegisterBuiltInEffects();
    }

    protected override void OnManagerDestroy()
    {
        base.OnManagerDestroy();
        effects.Clear();
    }
    #endregion

    #region Effect Registration
    public void Register(BaseEffect effect)
    {
        if (effect == null)
        {
            Debug.LogError("[EffectManager] Cannot register null effect");
            return;
        }

        effects[effect.Key] = effect;
        Debug.Log($"[EffectManager] Registered effect: {effect.Key}");
    }

    public void Unregister(ModifierKey key)
    {
        if (effects.Remove(key))
        {
            Debug.Log($"[EffectManager] Unregistered effect: {key}");
        }
        else
        {
            Debug.LogWarning($"[EffectManager] Effect not found for unregister: {key}");
        }
    }

    public bool IsRegistered(ModifierKey key)
    {
        return effects.ContainsKey(key);
    }
    #endregion

    #region Effect Retrieval
    public bool TryGetEffect(ModifierKey key, out BaseEffect effect)
    {
        return effects.TryGetValue(key, out effect);
    }

    public BaseEffect GetEffect(ModifierKey key)
    {
        if (!effects.TryGetValue(key, out var effect))
            throw new KeyNotFoundException($"Effect not found: {key}");
        return effect;
    }

    public T GetEffect<T>(ModifierKey key) where T : BaseEffect
    {
        BaseEffect effect = GetEffect(key);

        if (effect is T typedEffect)
            return typedEffect;
        else
            throw new InvalidCastException($"Effect {key} is not of type {typeof(T).Name}");
    }

    public ModifierEffect GetModifierEffect(ModifierKey key)
    {
        return GetEffect<ModifierEffect>(key);
    }

    public DirectEffect GetDirectEffect(ModifierKey key)
    {
        return GetEffect<DirectEffect>(key);
    }

    public ComplexEffect GetComplexEffect(ModifierKey key)
    {
        return GetEffect<ComplexEffect>(key);
    }
    #endregion

    #region Built-in Effects
    private void RegisterBuiltInEffects()
    {
        Debug.Log("[EffectManager] Registering built-in effects...");

        //// 기존 내장 효과들 등록
        //Register(EffectBuilder.DefineModifier(EffectId.Exhaust, EffectApplyMode.Timed)
        //        .Add("MoveSpeed", ModifierType.Multiplier, 0.7f)
        //        .Duration(2f)
        //        .Build());

        //Register(EffectBuilder.DefineModifier(EffectId.Ghost, EffectApplyMode.Timed)
        //        .Add("MoveSpeed", ModifierType.Multiplier, 1.8f)
        //        .Duration(10f)
        //        .Build());

        //Register(EffectBuilder.DefineModifier(EffectId.Slow, EffectApplyMode.Timed)
        //        .Add("MoveSpeed", ModifierType.Multiplier, 0.2f)
        //        .Duration(2f)
        //        .Build());

        Debug.Log("[EffectManager] Built-in effects registered successfully");
    }
    #endregion

    #region Effect Application Helpers
    /// <summary>
    /// 효과를 대상에게 직접 적용하는 헬퍼 메서드
    /// </summary>
    public void ApplyEffect(ModifierKey key, IModelOwner target)
    {
        if (TryGetEffect(key, out var effect))
        {
            effect.ApplyTo(target);
        }
        else
        {
            Debug.LogWarning($"[EffectManager] Cannot apply unknown effect: {key}");
        }
    }

    /// <summary>
    /// 효과를 대상에서 제거하는 헬퍼 메서드
    /// </summary>
    public void RemoveEffect(ModifierKey key, IModelOwner target)
    {
        if (TryGetEffect(key, out var effect))
        {
            effect.RemoveFrom(target);
        }
        else
        {
            Debug.LogWarning($"[EffectManager] Cannot remove unknown effect: {key}");
        }
    }
    #endregion

    #region Debug & Utilities
    /// <summary>
    /// 등록된 모든 효과의 키 목록 반환
    /// </summary>
    public IEnumerable<ModifierKey> GetAllEffectKeys()
    {
        return effects.Keys;
    }

    /// <summary>
    /// 등록된 효과 수 반환
    /// </summary>
    public int EffectCount => effects.Count;

    /// <summary>
    /// 효과 타입별 개수 반환
    /// </summary>
    public Dictionary<Type, int> GetEffectTypeStats()
    {
        var stats = new Dictionary<Type, int>();

        foreach (var effect in effects.Values)
        {
            var type = effect.GetType();
            stats[type] = stats.GetValueOrDefault(type) + 1;
        }

        return stats;
    }

#if UNITY_EDITOR
    [Header("Debug Info")]
    [SerializeField, TextArea(3, 10)] private string debugInfo;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (Application.isPlaying)
        {
            var typeStats = GetEffectTypeStats();
            var statsText = typeStats.Count > 0
                ? string.Join("\n", typeStats.Select(kvp => $"{kvp.Key.Name}: {kvp.Value}"))
                : "No effects registered";

            debugInfo = $"Total Effects: {EffectCount}\n" +
                       $"Effect Types:\n{statsText}";
        }
    }
#endif
    #endregion
}