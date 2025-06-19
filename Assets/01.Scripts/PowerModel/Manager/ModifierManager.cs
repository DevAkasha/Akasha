using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ModifierManager : ManagerBase
{
    public override int InitializationPriority => 50;

    private readonly Dictionary<int, ModifierContainer> instanceContainers = new();
    private readonly Dictionary<int, int> instanceOrders = new();

    public int TotalContainers => instanceContainers.Count;
    public int TotalModifiers
    {
        get
        {
            int total = 0;
            foreach (var container in instanceContainers.Values)
                total += container.Count;
            return total;
        }
    }

    protected override void OnManagerAwake()
    {
        Debug.Log("[ModifierManager] Initialized - Global modifier system ready");
    }

    protected override void OnManagerDestroy()
    {
        instanceContainers.Clear();
        instanceOrders.Clear();
        Debug.Log("[ModifierManager] Destroyed - All modifier data cleared");
    }

    public int RegisterInstance()
    {
        int instanceId = GetHashCode() ^ Time.frameCount;
        while (instanceContainers.ContainsKey(instanceId))
            instanceId++;

        instanceContainers[instanceId] = new ModifierContainer();
        instanceOrders[instanceId] = 0;

        return instanceId;
    }

    public void UnregisterInstance(int instanceId)
    {
        instanceContainers.Remove(instanceId);
        instanceOrders.Remove(instanceId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ModifierContainer GetContainer(int instanceId)
    {
        if (!instanceContainers.TryGetValue(instanceId, out var container))
            throw new ArgumentException($"Instance {instanceId} not registered");
        return container;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetModifier(int instanceId, ModifierType type, ModifierKey key, float value)
    {
        if (!key.IsValid)
            throw new ArgumentException("Invalid modifier key");

        var container = GetContainer(instanceId);
        var order = instanceOrders[instanceId]++;
        container.AddModifier(key, type, value, order);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveModifier(int instanceId, ModifierKey key)
    {
        var container = GetContainer(instanceId);
        return container.RemoveModifier(key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveModifier(int instanceId, ModifierType type, ModifierKey key)
    {
        var container = GetContainer(instanceId);
        if (container.TryGetModifier(key, out var data) && data.Type == type)
        {
            return container.RemoveModifier(key);
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RemoveModifiersByPredicate(int instanceId, Func<ModifierData, bool> predicate)
    {
        var container = GetContainer(instanceId);
        return container.RemoveByPredicate(predicate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RemoveAllModifiersOfType(int instanceId, ModifierType type)
    {
        var container = GetContainer(instanceId);
        return container.RemoveByType(type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(int instanceId)
    {
        var container = GetContainer(instanceId);
        container.Clear();
        instanceOrders[instanceId] = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(int instanceId, ModifierKey key)
    {
        var container = GetContainer(instanceId);
        return container.ContainsKey(key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetModifier(int instanceId, ModifierKey key, out ModifierData data)
    {
        var container = GetContainer(instanceId);
        return container.TryGetModifier(key, out data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CalculateValue(int instanceId, float baseValue)
    {
        var container = GetContainer(instanceId);

        float originSum = baseValue + container.GetTypeSum(ModifierType.OriginAdd);
        float addMultiplier = 1f + container.GetTypeSum(ModifierType.AddMultiplier);
        float multiplier = container.GetTypeProduct(ModifierType.Multiplier);
        float finalAdd = container.GetTypeSum(ModifierType.FinalAdd);

        return (originSum * addMultiplier * multiplier) + finalAdd;
    }

    public (float originSum, float addMul, float mul, float finalAdd) GetCalculationComponents(int instanceId, float baseValue)
    {
        var container = GetContainer(instanceId);

        float originSum = baseValue + container.GetTypeSum(ModifierType.OriginAdd);
        float addMul = 1f + container.GetTypeSum(ModifierType.AddMultiplier);
        float mul = container.GetTypeProduct(ModifierType.Multiplier);
        float finalAdd = container.GetTypeSum(ModifierType.FinalAdd);

        return (originSum, addMul, mul, finalAdd);
    }

    public Span<ModifierData> GetAllModifiers(int instanceId)
    {
        var container = GetContainer(instanceId);
        return container.GetAllAsSpan();
    }

    public IEnumerable<ModifierData> GetModifiersByType(int instanceId, ModifierType type)
    {
        var container = GetContainer(instanceId);
        return container.GetByType(type);
    }

    public string GetDebugFormula(int instanceId, float baseValue)
    {
        var (originSum, addMul, mul, finalAdd) = GetCalculationComponents(instanceId, baseValue);
        float result = CalculateValue(instanceId, baseValue);

        return $"({originSum:F2}) * {addMul:F2} * {mul:F2} + {finalAdd:F2} = {result:F2}";
    }

    public string GetDetailedDebugInfo(int instanceId, string fieldName, float baseValue)
    {
        var container = GetContainer(instanceId);
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"ModifierManager Instance {instanceId} for '{fieldName}'");
        sb.AppendLine($"Base Value: {baseValue}");
        sb.AppendLine($"Container: {container.GetDebugInfo()}");
        sb.AppendLine($"Formula: {GetDebugFormula(instanceId, baseValue)}");
        sb.AppendLine("Applied Modifiers:");

        var modifiers = GetAllModifiers(instanceId);
        for (int i = 0; i < modifiers.Length; i++)
        {
            sb.AppendLine($"  {modifiers[i]}");
        }

        return sb.ToString();
    }

    public void ValidateIntegrity(int instanceId)
    {
        var container = GetContainer(instanceId);

        if (container.Count < 0)
            throw new InvalidOperationException($"Container {instanceId} count is negative");

        var allModifiers = GetAllModifiers(instanceId);
        for (int i = 0; i < allModifiers.Length; i++)
        {
            if (!allModifiers[i].Key.IsValid)
                throw new InvalidOperationException($"Invalid key found at index {i} in instance {instanceId}");
        }
    }

    public void ClearAllInstances()
    {
        foreach (var kvp in instanceContainers)
        {
            kvp.Value.Clear();
        }
        foreach (var key in instanceOrders.Keys.ToArray())
        {
            instanceOrders[key] = 0;
        }
    }

    public Dictionary<int, int> GetInstanceStatistics()
    {
        var stats = new Dictionary<int, int>();
        foreach (var kvp in instanceContainers)
        {
            stats[kvp.Key] = kvp.Value.Count;
        }
        return stats;
    }

#if UNITY_EDITOR
    [Header("Debug Statistics")]
    [SerializeField, TextArea(3, 8)] private string debugStatistics;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (Application.isPlaying)
        {
            var stats = GetInstanceStatistics();
            var totalModifiers = TotalModifiers;

            debugStatistics = $"Total Instances: {TotalContainers}\n" +
                            $"Total Modifiers: {totalModifiers}\n" +
                            $"Average per Instance: {(TotalContainers > 0 ? (float)totalModifiers / TotalContainers : 0):F1}\n" +
                            $"Registered Keys: {ModifierKey.GetRegisteredCount()}";
        }
    }
#endif
}