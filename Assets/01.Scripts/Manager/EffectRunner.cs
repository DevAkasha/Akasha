using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EffectRunner : ManagerBase
{
    public override int InitializationPriority => 65;

    private readonly Dictionary<(ModifierKey, IModelOwner), Coroutine> activeEffects = new();
    private EffectManager effectManager;

    protected override void OnManagerAwake()
    {
        base.OnManagerAwake();
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
        CleanupEffectsForScene(scene);
    }

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
        if (effect.RemoveTrigger != null)
        {
            yield return CheckRemoveTrigger(effect.RemoveTrigger);
        }
        else
        {
            yield return new WaitForSeconds(effect.Duration);
        }

        effect.RemoveFrom(target);
        Debug.Log($"[EffectRunner] <color=yellow>Effect END</color> - {effect.Key}");

        var key = (effect.Key, target);
        activeEffects.Remove(key);
    }

    private IEnumerator CheckRemoveTrigger(Func<bool> trigger)
    {
        while (!trigger())
        {
            yield return new WaitForSeconds(0.2f);
        }
    }

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

        var modifiers = effect.Modifiers;

        while (elapsedTime < duration)
        {
            float normalizedTime = elapsedTime / duration;
            float interpolatedValue = effect.Interpolator.Invoke(normalizedTime);

            foreach (var modifiable in target.GetModifiables())
            {
                if (modifiable is not IRxField rxField)
                    continue;

                foreach (var modifier in modifiers)
                {
                    if (!string.Equals(rxField.FieldName, modifier.FieldName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (modifiable is IModifiable mod)
                    {
                        try
                        {
                            mod.SetModifier(key, modifier.Type, interpolatedValue);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[EffectRunner] Failed to set interpolated modifier: {e.Message}");
                        }
                    }
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        foreach (var modifiable in target.GetModifiables())
        {
            modifiable.RemoveModifier(key, 0);
        }

        Debug.Log($"[EffectRunner] Interpolated effect removed: {key}");
    }

    public bool CancelEffect(ModifierKey effectKey, IModelOwner target)
    {
        var key = (effectKey, target);

        if (activeEffects.TryGetValue(key, out var coroutine))
        {
            StopCoroutine(coroutine);
            activeEffects.Remove(key);

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
        var keysToRemove = new List<(ModifierKey, IModelOwner)>();

        foreach (var pair in activeEffects)
        {
            var (effectKey, effectTarget) = pair.Key;

            if (effectTarget == target)
            {
                keysToRemove.Add(pair.Key);
                StopCoroutine(pair.Value);

                if (effectManager != null && effectManager.TryGetEffect(effectKey, out var effect))
                {
                    effect.RemoveFrom(target);
                }
            }
        }

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

    private void CleanupEffectsForScene(Scene scene)
    {
        var keysToRemove = new List<(ModifierKey, IModelOwner)>();

        foreach (var pair in activeEffects)
        {
            var (effectKey, target) = pair.Key;

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
}