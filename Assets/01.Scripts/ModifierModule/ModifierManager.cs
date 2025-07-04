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
    private readonly Dictionary<int, Dictionary<ModifierKey, int>> instanceStackCounters = new();

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
        Debug.Log("[ModifierManager] Initialized - Global modifier system with stacking support ready");
    }

    protected override void OnManagerDestroy()
    {
        instanceContainers.Clear();
        instanceOrders.Clear();
        instanceStackCounters.Clear();
        Debug.Log("[ModifierManager] Destroyed - All modifier data cleared");
    }

    public int RegisterInstance()
    {
        int instanceId = GetHashCode() ^ Time.frameCount;
        while (instanceContainers.ContainsKey(instanceId))
            instanceId++;

        instanceContainers[instanceId] = new ModifierContainer();
        instanceOrders[instanceId] = 0;
        instanceStackCounters[instanceId] = new Dictionary<ModifierKey, int>();

        return instanceId;
    }

    public void UnregisterInstance(int instanceId)
    {
        instanceContainers.Remove(instanceId);
        instanceOrders.Remove(instanceId);
        instanceStackCounters.Remove(instanceId);
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
        SetModifier(instanceId, type, key, value, StackBehavior.ReplaceLatest);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetModifier(int instanceId, ModifierType type, ModifierKey key, float value, StackBehavior stackBehavior, int stackId = 0)
    {
        if (!key.IsValid)
            throw new ArgumentException("Invalid modifier key");

        var container = GetContainer(instanceId);
        var order = instanceOrders[instanceId]++;
        container.AddModifier(key, type, value, order, stackBehavior, stackId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStackModifier(int instanceId, ModifierType type, ModifierKey key, float value, int stackId = -1)
    {
        if (stackId < 0)
        {
            stackId = GetNextStackId(instanceId, key);
        }

        SetModifier(instanceId, type, key, value, StackBehavior.Stack, stackId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetNextStackId(int instanceId, ModifierKey baseKey)
    {
        if (!instanceStackCounters.TryGetValue(instanceId, out var stackCounters))
        {
            stackCounters = new Dictionary<ModifierKey, int>();
            instanceStackCounters[instanceId] = stackCounters;
        }

        if (!stackCounters.TryGetValue(baseKey, out var currentId))
        {
            currentId = 0;
        }

        stackCounters[baseKey] = currentId + 1;
        return currentId + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveModifier(int instanceId, ModifierKey key, int stackId = 0)
    {
        var container = GetContainer(instanceId);
        return container.RemoveModifier(key, stackId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveModifier(int instanceId, ModifierType type, ModifierKey key)
    {
        var container = GetContainer(instanceId);
        return container.RemoveByBaseKeyAndType(key, type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveByBaseKey(int instanceId, ModifierKey baseKey)
    {
        var container = GetContainer(instanceId);
        return container.RemoveByBaseKey(baseKey);
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

        if (instanceStackCounters.TryGetValue(instanceId, out var stackCounters))
        {
            stackCounters.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(int instanceId, ModifierKey key, int stackId = 0)
    {
        var container = GetContainer(instanceId);
        return container.ContainsKey(key, stackId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsBaseKey(int instanceId, ModifierKey baseKey)
    {
        var container = GetContainer(instanceId);
        return container.ContainsBaseKey(baseKey);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetModifier(int instanceId, ModifierKey key, out ModifierData data, int stackId = 0)
    {
        var container = GetContainer(instanceId);
        return container.TryGetModifier(key, out data, stackId);
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

    public IEnumerable<ModifierData> GetModifiersByBaseKey(int instanceId, ModifierKey baseKey)
    {
        var container = GetContainer(instanceId);
        return container.GetByBaseKey(baseKey);
    }

    public int GetStackCount(int instanceId, ModifierKey baseKey)
    {
        var container = GetContainer(instanceId);
        return container.GetStackCount(baseKey);
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

        var stackStats = GetStackStatistics(instanceId);
        if (stackStats.Count > 0)
        {
            sb.AppendLine("Stack Statistics:");
            foreach (var kvp in stackStats)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value} stacks");
            }
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
        foreach (var kvp in instanceStackCounters.Values)
        {
            kvp.Clear();
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

    public Dictionary<ModifierKey, int> GetStackStatistics(int instanceId)
    {
        var container = GetContainer(instanceId);
        var stackStats = new Dictionary<ModifierKey, int>();

        var allModifiers = container.GetAllAsSpan();
        for (int i = 0; i < allModifiers.Length; i++)
        {
            var key = allModifiers[i].Key;
            stackStats[key] = stackStats.GetValueOrDefault(key) + 1;
        }

        return stackStats;
    }

    public Dictionary<int, Dictionary<ModifierKey, int>> GetAllStackStatistics()
    {
        var allStats = new Dictionary<int, Dictionary<ModifierKey, int>>();
        foreach (var instanceId in instanceContainers.Keys)
        {
            allStats[instanceId] = GetStackStatistics(instanceId);
        }
        return allStats;
    }

#if UNITY_EDITOR


    protected override void OnValidate()
    {
        base.OnValidate();

        if (Application.isPlaying)
        {
            var stats = GetInstanceStatistics();
            var stackStats = GetAllStackStatistics();
            var totalModifiers = TotalModifiers;

            var stackInfo = "";
            foreach (var kvp in stackStats)
            {
                var instanceId = kvp.Key;
                var stacks = kvp.Value;
                if (stacks.Count > 0)
                {
                    var stackDetails = string.Join(", ", stacks.Select(s => $"{s.Key}({s.Value})"));
                    stackInfo += $"Instance {instanceId}: {stackDetails}\n";
                }
            }

            debugInfo = $"Total Instances: {TotalContainers}\n" +
                            $"Total Modifiers: {totalModifiers}\n" +
                            $"Average per Instance: {(TotalContainers > 0 ? (float)totalModifiers / TotalContainers : 0):F1}\n" +
                            $"Registered Keys: {ModifierKey.GetRegisteredCount()}\n" +
                            $"Stack Info:\n{stackInfo}";
        }
    }
#endif
}