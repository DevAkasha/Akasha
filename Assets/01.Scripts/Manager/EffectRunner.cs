using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EffectRunner : ManagerBase
{
    public override int InitializationPriority => 15; // EffectManager 다음에 초기화

    private readonly Dictionary<(ModifierKey, IModelOwner), Coroutine> activeEffects = new();
    private EffectManager effectManager;

    #region Manager Lifecycle
    protected override void OnManagerAwake()
    {
        base.OnManagerAwake();
        // EffectManager는 우선도가 더 높아서 이미 초기화되어 있음
        effectManager = GameManager.Effect;

        if (effectManager == null)
        {
            Debug.LogError("[EffectRunner] EffectManager not found! EffectRunner requires EffectManager.");
        }
    }

    protected override void OnManagerDestroy()
    {
        base.OnManagerDestroy();
        CancelAllEffects();
    }

    public override void OnSceneUnloaded(Scene scene)
    {
        base.OnSceneUnloaded(scene);

        // 씬이 언로드될 때 해당 씬의 객체들과 관련된 효과들 정리
        CleanupEffectsForScene(scene);
    }
    #endregion

    #region Timed Effects
    public void RegisterTimedEffect(ModifierEffect effect, IModelOwner target)
    {
        if (target == null)
        {
            Debug.LogError("[EffectRunner] Cannot register effect for null target");
            return;
        }

        if (!effect.Condition())
        {
            Debug.LogWarning($"[EffectRunner] Condition not met for effect: {effect.Key}");
            return;
        }

        if (TryStartEffect(effect, target, out var coroutine))
        {
            Debug.Log($"[EffectRunner] <color=cyan>Effect START</color> - {effect.Key}, Duration: {effect.Duration}s");

            if (!effect.Stackable)
            {
                activeEffects[(effect.Key, target)] = coroutine;
            }
        }
    }

    private bool TryStartEffect(ModifierEffect effect, IModelOwner target, out Coroutine coroutine)
    {
        var key = (effect.Key, target);

        if (activeEffects.TryGetValue(key, out var existing))
        {
            if (!effect.Stackable)
            {
                if (effect.RefreshOnDuplicate)
                {
                    Debug.Log($"[EffectRunner] Refreshing effect: {effect.Key}");
                    StopCoroutine(existing);
                    coroutine = StartCoroutine(RunEffect(effect, target));
                    return true;
                }
                else
                {
                    Debug.Log($"[EffectRunner] Skipping duplicate effect: {effect.Key}");
                    coroutine = null;
                    return false;
                }
            }
        }

        coroutine = StartCoroutine(RunEffect(effect, target));
        return true;
    }

    private IEnumerator RunEffect(ModifierEffect effect, IModelOwner target)
    {
        // 효과가 이미 적용되었다고 가정 (ModifierEffect.ApplyTo 메서드에서 적용됨)

        // 제거 트리거가 있는 경우 해당 조건 체크
        if (effect.RemoveTrigger != null)
        {
            yield return CheckRemoveTrigger(effect.RemoveTrigger);
        }
        // 그렇지 않으면 지정된 지속 시간 동안 대기
        else
        {
            yield return new WaitForSeconds(effect.Duration);
        }

        // 효과 제거
        effect.RemoveFrom(target);
        Debug.Log($"[EffectRunner] <color=yellow>Effect END</color> - {effect.Key}");

        // 활성 효과 목록에서 제거
        var key = (effect.Key, target);
        activeEffects.Remove(key);
    }

    private IEnumerator CheckRemoveTrigger(Func<bool> trigger)
    {
        // 트리거 조건이 참이 될 때까지 주기적으로 체크
        while (!trigger())
        {
            yield return new WaitForSeconds(0.2f);
        }
    }
    #endregion

    #region Interpolated Effects
    public void RegisterInterpolatedEffect(ModifierEffect effect, IModelOwner target)
    {
        if (target == null)
        {
            Debug.LogError("[EffectRunner] Cannot register interpolated effect for null target");
            return;
        }

        if (!effect.IsInterpolated)
        {
            Debug.LogError($"[EffectRunner] Effect {effect.Key} is not interpolated");
            return;
        }

        var modifiableTarget = target.GetBaseModel() as IModifiableTarget;
        if (modifiableTarget == null)
        {
            Debug.LogError($"[EffectRunner] Target {target} does not implement IModifiableTarget");
            return;
        }

        StartCoroutine(RunInterpolatedEffect(effect, modifiableTarget));
    }

    private IEnumerator RunInterpolatedEffect(ModifierEffect effect, IModifiableTarget target)
    {
        float duration = effect.Duration;
        float elapsedTime = 0f;
        ModifierKey key = effect.Key;

        // ModifierEffect에 등록된 필드 목록
        var modifiers = effect.Modifiers;

        // 진행 시간(0~duration)에 따라 보간값 적용
        while (elapsedTime < duration)
        {
            // 정규화된 시간 값 (0~1 사이)
            float normalizedTime = elapsedTime / duration;

            // 보간 함수를 통해 현재 시간에 해당하는 값 계산
            object interpolatedValue = effect.Interpolator.Invoke(normalizedTime);

            // 모든 수정 가능한 필드에 보간된 값 적용
            foreach (var modifiable in target.GetModifiables())
            {
                if (modifiable is not IRxField rxField)
                    continue;

                foreach (var modifier in modifiers)
                {
                    // 필드명이 일치할 경우에만 적용
                    if (!string.Equals(rxField.FieldName, modifier.FieldName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (modifiable is IRxModBase mod)
                    {
                        try
                        {
                            mod.SetModifier(modifier.Type, key, interpolatedValue);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[EffectRunner] Failed to set interpolated modifier: {e.Message}");
                        }
                    }
                }
            }

            // 시간 업데이트
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 종료 시 모든 modifier 제거
        foreach (var modifiable in target.GetModifiables())
        {
            modifiable.RemoveModifier(key);
        }

        Debug.Log($"[EffectRunner] Interpolated effect removed: {key}");
    }
    #endregion

    #region Effect Control
    public bool CancelEffect(ModifierKey effectKey, IModelOwner target)
    {
        var key = (effectKey, target);

        if (activeEffects.TryGetValue(key, out var coroutine))
        {
            StopCoroutine(coroutine);
            activeEffects.Remove(key);

            // 효과 제거
            if (effectManager != null && effectManager.TryGetEffect(effectKey, out var effect))
            {
                effect.RemoveFrom(target);
                Debug.Log($"[EffectRunner] <color=orange>Effect CANCELED</color> - {effectKey}");
                return true;
            }
        }

        return false;
    }

    public void CancelAllEffects(IModelOwner target)
    {
        // 대상과 관련된 모든 키 수집
        var keysToRemove = new List<(ModifierKey, IModelOwner)>();

        foreach (var pair in activeEffects)
        {
            var (effectKey, effectTarget) = pair.Key;

            if (effectTarget == target)
            {
                keysToRemove.Add(pair.Key);
                StopCoroutine(pair.Value);

                // 효과 제거
                if (effectManager != null && effectManager.TryGetEffect(effectKey, out var effect))
                {
                    effect.RemoveFrom(target);
                }
            }
        }

        // 활성 효과 목록에서 제거
        foreach (var key in keysToRemove)
        {
            activeEffects.Remove(key);
        }

        if (keysToRemove.Count > 0)
        {
            Debug.Log($"[EffectRunner] <color=orange>Canceled {keysToRemove.Count} effects</color> for {target}");
        }
    }

    public void CancelAllEffects()
    {
        Debug.Log($"[EffectRunner] Canceling all {activeEffects.Count} active effects");

        foreach (var coroutine in activeEffects.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }

        activeEffects.Clear();
    }
    #endregion

    #region Scene Cleanup
    private void CleanupEffectsForScene(Scene scene)
    {
        var keysToRemove = new List<(ModifierKey, IModelOwner)>();

        foreach (var pair in activeEffects)
        {
            var (effectKey, target) = pair.Key;

            // 대상이 언로드된 씬에 속하는지 확인
            if (target != null && target is MonoBehaviour mb && mb.gameObject.scene == scene)
            {
                keysToRemove.Add(pair.Key);
                StopCoroutine(pair.Value);
            }
        }

        foreach (var key in keysToRemove)
        {
            activeEffects.Remove(key);
        }

        if (keysToRemove.Count > 0)
        {
            Debug.Log($"[EffectRunner] Cleaned up {keysToRemove.Count} effects for scene: {scene.name}");
        }
    }
    #endregion

    #region Query & Debug
    public bool HasActiveEffect(ModifierKey effectKey, IModelOwner target)
    {
        return activeEffects.ContainsKey((effectKey, target));
    }

    public List<ModifierKey> GetActiveEffectKeys(IModelOwner target)
    {
        var keys = new List<ModifierKey>();

        foreach (var pair in activeEffects)
        {
            var (effectKey, effectTarget) = pair.Key;

            if (effectTarget == target)
            {
                keys.Add(effectKey);
            }
        }

        return keys;
    }

    public int ActiveEffectCount => activeEffects.Count;

    public Dictionary<ModifierKey, int> GetActiveEffectStats()
    {
        var stats = new Dictionary<ModifierKey, int>();

        foreach (var (effectKey, _) in activeEffects.Keys)
        {
            stats[effectKey] = stats.GetValueOrDefault(effectKey) + 1;
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
            var effectStats = GetActiveEffectStats();
            var statsText = effectStats.Count > 0
                ? string.Join("\n", effectStats.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
                : "No active effects";

            debugInfo = $"Active Effects: {ActiveEffectCount}\n" +
                       $"Effect Stats:\n{statsText}";
        }
    }
#endif
    #endregion
}