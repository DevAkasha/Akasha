using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ContainerManager<T> : ManagerBase where T : AggregateRoot
{
    [Header("Container Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool enableInstanceTracking = true;
    [SerializeField] private bool enableGroupManagement = true;

    #region Registry Management
    // InstanceID를 키로 하는 등록된 객체 관리
    private readonly Dictionary<int, T> registeredItems = new();
    private readonly Dictionary<int, ContainerItemInfo> itemInfos = new();

    // 그룹 관리
    private readonly Dictionary<string, HashSet<int>> groups = new();
    private readonly Dictionary<int, HashSet<string>> itemGroups = new();

    // 태그 시스템
    private readonly Dictionary<string, HashSet<int>> tags = new();
    private readonly Dictionary<int, HashSet<string>> itemTags = new();

    // 의존성 관리
    private readonly Dictionary<int, HashSet<int>> dependencies = new(); // key가 value들에 의존
    private readonly Dictionary<int, HashSet<int>> dependents = new();   // key를 value들이 의존

    // 상태 추적
    private readonly Dictionary<int, ItemState> itemStates = new();

    // 이벤트 시스템
    private readonly Dictionary<int, List<Action<T>>> itemEvents = new();

    /// <summary>
    /// 객체를 관리 대상으로 등록
    /// </summary>
    public void Register(T item, ContainerItemOptions options = null)
    {
        if (item == null)
        {
            LogWarning("Cannot register null item");
            return;
        }

        int instanceId = item.GetInstanceID();

        if (registeredItems.ContainsKey(instanceId))
        {
            LogWarning($"Item already registered: {item.name} (ID: {instanceId})");
            return;
        }

        registeredItems[instanceId] = item;

        if (enableInstanceTracking)
        {
            var info = new ContainerItemInfo
            {
                InstanceId = instanceId,
                RegisterTime = Time.time,
                Scene = item.gameObject.scene,
                OriginalName = item.name,
                Type = item.GetType()
            };

            itemInfos[instanceId] = info;
            itemStates[instanceId] = ItemState.Active;
        }

        // 옵션 처리
        if (options != null)
        {
            ProcessRegistrationOptions(instanceId, options);
        }

        Log($"Registered: {item.name} (ID: {instanceId})");

        // 등록 이벤트 발생
        OnItemRegistered?.Invoke(item);
    }

    /// <summary>
    /// AggregateRoot의 메타데이터를 자동으로 사용하여 등록
    /// </summary>
    public void RegisterWithAutoMetadata(T item)
    {
        if (item == null)
        {
            LogWarning("Cannot register null AggregateRoot");
            return;
        }

        var options = new ContainerItemOptions
        {
            Groups = item.GetGroupArray().ToList(),
            Tags = item.GetTagStringArray().ToList()
        };

        // AggregateRoot의 상세 정보로 ItemInfo 업데이트
        Register(item, options);

        // AggregateRoot 전용 정보 추가
        if (enableInstanceTracking && itemInfos.TryGetValue(item.GetInstanceID(), out var info))
        {
            info.AggregateId = item.AggregateId;
            info.CreatedTime = item.CreatedTime;
            info.CreatedScene = item.CreatedScene;
            info.CustomMetadata = item.GetAllMetadata();
        }
    }

    private void ProcessRegistrationOptions(int instanceId, ContainerItemOptions options)
    {
        // 그룹 추가
        foreach (var group in options.Groups)
        {
            AddToGroup(instanceId, group);
        }

        // 태그 추가
        foreach (var tag in options.Tags)
        {
            AddTag(instanceId, tag);
        }

        // 의존성 추가
        foreach (var depId in options.Dependencies)
        {
            AddDependency(instanceId, depId);
        }
    }

    /// <summary>
    /// 객체를 관리 대상에서 해제
    /// </summary>
    public void Unregister(T item)
    {
        if (item == null)
        {
            LogWarning("Cannot unregister null item");
            return;
        }

        int instanceId = item.GetInstanceID();
        UnregisterById(instanceId);
    }

    public void UnregisterById(int instanceId)
    {
        if (!registeredItems.TryGetValue(instanceId, out var item))
        {
            LogWarning($"Item not found for unregister: ID {instanceId}");
            return;
        }

        // 의존성 체크
        if (HasDependents(instanceId))
        {
            var dependentNames = GetDependents(instanceId)
                .Select(id => registeredItems.TryGetValue(id, out var dep) ? dep.name : $"ID:{id}")
                .ToArray();
            LogWarning($"Cannot unregister {item.name} - has dependents: {string.Join(", ", dependentNames)}");
            return;
        }

        registeredItems.Remove(instanceId);
        CleanupItemData(instanceId);

        Log($"Unregistered: {item.name} (ID: {instanceId})");

        // 해제 이벤트 발생
        OnItemUnregistered?.Invoke(item);
    }

    private void CleanupItemData(int instanceId)
    {
        itemInfos.Remove(instanceId);
        itemStates.Remove(instanceId);
        itemEvents.Remove(instanceId);

        // 그룹에서 제거
        if (itemGroups.TryGetValue(instanceId, out var groupSet))
        {
            foreach (var group in groupSet)
            {
                groups[group]?.Remove(instanceId);
            }
            itemGroups.Remove(instanceId);
        }

        // 태그에서 제거
        if (itemTags.TryGetValue(instanceId, out var tagSet))
        {
            foreach (var tag in tagSet)
            {
                tags[tag]?.Remove(instanceId);
            }
            itemTags.Remove(instanceId);
        }

        // 의존성 정리
        RemoveAllDependencies(instanceId);
    }
    #endregion

    #region Group Management
    public void AddToGroup(int instanceId, string groupName)
    {
        if (!enableGroupManagement) return;

        if (!groups.ContainsKey(groupName))
        {
            groups[groupName] = new HashSet<int>();
        }

        if (!itemGroups.ContainsKey(instanceId))
        {
            itemGroups[instanceId] = new HashSet<string>();
        }

        groups[groupName].Add(instanceId);
        itemGroups[instanceId].Add(groupName);

        Log($"Added ID {instanceId} to group '{groupName}'");
    }

    public void RemoveFromGroup(int instanceId, string groupName)
    {
        if (groups.TryGetValue(groupName, out var group))
        {
            group.Remove(instanceId);
        }

        if (itemGroups.TryGetValue(instanceId, out var groupSet))
        {
            groupSet.Remove(groupName);
        }

        Log($"Removed ID {instanceId} from group '{groupName}'");
    }

    public IEnumerable<T> GetGroup(string groupName)
    {
        if (!groups.TryGetValue(groupName, out var group))
            return Enumerable.Empty<T>();

        return group.Select(id => registeredItems.TryGetValue(id, out var item) ? item : null)
                   .Where(item => item != null);
    }

    public void SetGroupActive(string groupName, bool active)
    {
        foreach (var item in GetGroup(groupName))
        {
            SetActive(item, active);
        }
        Log($"Set group '{groupName}' active: {active}");
    }

    public void DestroyGroup(string groupName)
    {
        var items = GetGroup(groupName).ToArray();
        foreach (var item in items)
        {
            if (item != null)
            {
                UnityEngine.Object.Destroy(item.gameObject);
            }
        }
        Log($"Destroyed group '{groupName}': {items.Length} items");
    }
    #endregion

    #region Tag System
    public void AddTag(int instanceId, string tag)
    {
        if (!tags.ContainsKey(tag))
        {
            tags[tag] = new HashSet<int>();
        }

        if (!itemTags.ContainsKey(instanceId))
        {
            itemTags[instanceId] = new HashSet<string>();
        }

        tags[tag].Add(instanceId);
        itemTags[instanceId].Add(tag);
    }

    public void RemoveTag(int instanceId, string tag)
    {
        if (tags.TryGetValue(tag, out var tagSet))
        {
            tagSet.Remove(instanceId);
        }

        if (itemTags.TryGetValue(instanceId, out var itemTagSet))
        {
            itemTagSet.Remove(tag);
        }
    }

    public bool HasTag(int instanceId, string tag)
    {
        return itemTags.TryGetValue(instanceId, out var tagSet) && tagSet.Contains(tag);
    }

    public IEnumerable<T> GetByTag(string tag)
    {
        if (!tags.TryGetValue(tag, out var tagSet))
            return Enumerable.Empty<T>();

        return tagSet.Select(id => registeredItems.TryGetValue(id, out var item) ? item : null)
                    .Where(item => item != null);
    }

    public IEnumerable<T> GetByTags(params string[] tags)
    {
        var result = new HashSet<int>();
        bool first = true;

        foreach (var tag in tags)
        {
            if (this.tags.TryGetValue(tag, out var tagSet))
            {
                if (first)
                {
                    result.UnionWith(tagSet);
                    first = false;
                }
                else
                {
                    result.IntersectWith(tagSet);
                }
            }
            else
            {
                result.Clear();
                break;
            }
        }

        return result.Select(id => registeredItems.TryGetValue(id, out var item) ? item : null)
                    .Where(item => item != null);
    }
    #endregion

    #region Dependency Management
    public void AddDependency(int dependentId, int dependencyId)
    {
        if (!dependencies.ContainsKey(dependentId))
        {
            dependencies[dependentId] = new HashSet<int>();
        }

        if (!dependents.ContainsKey(dependencyId))
        {
            dependents[dependencyId] = new HashSet<int>();
        }

        dependencies[dependentId].Add(dependencyId);
        dependents[dependencyId].Add(dependentId);

        Log($"Added dependency: {dependentId} depends on {dependencyId}");
    }

    public void RemoveDependency(int dependentId, int dependencyId)
    {
        if (dependencies.TryGetValue(dependentId, out var deps))
        {
            deps.Remove(dependencyId);
        }

        if (dependents.TryGetValue(dependencyId, out var depts))
        {
            depts.Remove(dependentId);
        }
    }

    public void RemoveAllDependencies(int instanceId)
    {
        // 이 객체가 의존하는 것들 제거
        if (dependencies.TryGetValue(instanceId, out var deps))
        {
            foreach (var depId in deps.ToArray())
            {
                RemoveDependency(instanceId, depId);
            }
        }

        // 이 객체에 의존하는 것들 제거
        if (dependents.TryGetValue(instanceId, out var depts))
        {
            foreach (var deptId in depts.ToArray())
            {
                RemoveDependency(deptId, instanceId);
            }
        }
    }

    public bool HasDependents(int instanceId)
    {
        return dependents.TryGetValue(instanceId, out var depts) && depts.Count > 0;
    }

    public IEnumerable<int> GetDependencies(int instanceId)
    {
        return dependencies.TryGetValue(instanceId, out var deps) ? deps : Enumerable.Empty<int>();
    }

    public IEnumerable<int> GetDependents(int instanceId)
    {
        return dependents.TryGetValue(instanceId, out var depts) ? depts : Enumerable.Empty<int>();
    }

    public bool CanSafelyUnregister(int instanceId)
    {
        return !HasDependents(instanceId);
    }
    #endregion

    #region State Management
    public void SetItemState(int instanceId, ItemState state)
    {
        if (itemStates.TryGetValue(instanceId, out var oldState))
        {
            itemStates[instanceId] = state;
            Log($"Item {instanceId} state changed: {oldState} → {state}");

            if (registeredItems.TryGetValue(instanceId, out var item))
            {
                OnItemStateChanged?.Invoke(item, oldState, state);
            }
        }
    }

    public ItemState GetItemState(int instanceId)
    {
        return itemStates.TryGetValue(instanceId, out var state) ? state : ItemState.Unknown;
    }

    public IEnumerable<T> GetByState(ItemState state)
    {
        return itemStates.Where(kvp => kvp.Value == state)
                        .Select(kvp => registeredItems.TryGetValue(kvp.Key, out var item) ? item : null)
                        .Where(item => item != null);
    }
    #endregion

    #region Event System
    public void SubscribeToItem(int instanceId, Action<T> callback)
    {
        if (!itemEvents.ContainsKey(instanceId))
        {
            itemEvents[instanceId] = new List<Action<T>>();
        }

        itemEvents[instanceId].Add(callback);
    }

    public void UnsubscribeFromItem(int instanceId, Action<T> callback)
    {
        if (itemEvents.TryGetValue(instanceId, out var callbacks))
        {
            callbacks.Remove(callback);
        }
    }

    public void TriggerItemEvent(int instanceId)
    {
        if (itemEvents.TryGetValue(instanceId, out var callbacks) &&
            registeredItems.TryGetValue(instanceId, out var item))
        {
            foreach (var callback in callbacks)
            {
                try
                {
                    callback(item);
                }
                catch (Exception ex)
                {
                    LogError($"Error in item event callback: {ex.Message}");
                }
            }
        }
    }
    #endregion

    #region Advanced Queries
    /// <summary>
    /// 복잡한 조건으로 객체 검색
    /// </summary>
    public IEnumerable<T> FindItems(Func<ContainerItemInfo, bool> predicate)
    {
        return itemInfos.Values.Where(predicate)
                              .Select(info => registeredItems.TryGetValue(info.InstanceId, out var item) ? item : null)
                              .Where(item => item != null);
    }

    /// <summary>
    /// 씬별 객체 조회
    /// </summary>
    public IEnumerable<T> GetByScene(Scene scene)
    {
        return FindItems(info => info.Scene == scene);
    }

    /// <summary>
    /// 등록 시간 기준 조회
    /// </summary>
    public IEnumerable<T> GetByRegistrationTime(float minTime, float maxTime = float.MaxValue)
    {
        return FindItems(info => info.RegisterTime >= minTime && info.RegisterTime <= maxTime);
    }

    /// <summary>
    /// 타입별 조회 (상속 관계 고려)
    /// </summary>
    public IEnumerable<T> GetByType(Type type, bool includeSubclasses = true)
    {
        if (includeSubclasses)
        {
            return FindItems(info => type.IsAssignableFrom(info.Type));
        }
        else
        {
            return FindItems(info => info.Type == type);
        }
    }
    #endregion

    #region Batch Operations
    /// <summary>
    /// 배치 활성화/비활성화
    /// </summary>
    public void SetActiveByCondition(Func<T, bool> condition, bool active)
    {
        var items = registeredItems.Values.Where(condition).ToArray();
        foreach (var item in items)
        {
            SetActive(item, active);
        }
        Log($"Set {items.Length} items active: {active}");
    }

    /// <summary>
    /// 배치 상태 변경
    /// </summary>
    public void SetStateByCondition(Func<T, bool> condition, ItemState state)
    {
        var items = registeredItems.Values.Where(condition).ToArray();
        foreach (var item in items)
        {
            SetItemState(item.GetInstanceID(), state);
        }
        Log($"Set {items.Length} items to state: {state}");
    }

    /// <summary>
    /// 조건부 배치 삭제
    /// </summary>
    public void DestroyByCondition(Func<T, bool> condition, bool forceDependencyBreak = false)
    {
        var items = registeredItems.Values.Where(condition).ToArray();

        if (forceDependencyBreak)
        {
            foreach (var item in items)
            {
                RemoveAllDependencies(item.GetInstanceID());
            }
        }

        foreach (var item in items)
        {
            if (CanSafelyUnregister(item.GetInstanceID()))
            {
                UnityEngine.Object.Destroy(item.gameObject);
            }
        }

        Log($"Destroyed {items.Length} items");
    }
    #endregion

    #region Object Pooling
    // 타입별로 별도 풀 관리
    private readonly Dictionary<Type, Queue<T>> objectPools = new();
    private readonly Dictionary<Type, T> poolPrefabs = new();

    /// <summary>
    /// 풀에서 객체 가져오기 (없으면 새로 생성)
    /// </summary>
    public TSpecific GetFromPool<TSpecific>(TSpecific prefab) where TSpecific : T
    {
        if (prefab == null)
        {
            LogError("Cannot get from pool with null prefab");
            return null;
        }

        Type type = typeof(TSpecific);

        // 프리팹 등록 (첫 호출시)
        if (!poolPrefabs.ContainsKey(type))
        {
            poolPrefabs[type] = prefab;
            objectPools[type] = new Queue<T>();
            Log($"Created new pool for type: {type.Name}");
        }

        Queue<T> pool = objectPools[type];

        // 풀에서 재사용 가능한 객체 찾기
        T pooledItem = null;
        while (pool.Count > 0)
        {
            var candidate = pool.Dequeue();
            if (candidate != null && candidate.gameObject != null)
            {
                pooledItem = candidate;
                break;
            }
        }

        TSpecific result;

        if (pooledItem != null)
        {
            // 풀에서 재사용
            result = pooledItem as TSpecific;
            result.gameObject.SetActive(true);
            Log($"Reused from pool: {type.Name}");
        }
        else
        {
            // 새로 생성
            result = Instantiate(prefab);
            Log($"Created new instance: {type.Name}");
        }

        return result;
    }

    /// <summary>
    /// 객체를 풀에 반납
    /// </summary>
    public void ReturnToPool<TSpecific>(TSpecific item) where TSpecific : T
    {
        if (item == null)
        {
            LogWarning("Cannot return null item to pool");
            return;
        }

        Type type = typeof(TSpecific);

        if (!objectPools.ContainsKey(type))
        {
            LogWarning($"No pool exists for type: {type.Name}");
            return;
        }

        // 객체 비활성화 후 풀에 반납
        item.gameObject.SetActive(false);
        objectPools[type].Enqueue(item);

        Log($"Returned to pool: {type.Name}");
    }

    /// <summary>
    /// 특정 타입의 풀 정리
    /// </summary>
    public void ClearPool<TSpecific>() where TSpecific : T
    {
        Type type = typeof(TSpecific);

        if (objectPools.TryGetValue(type, out var pool))
        {
            int count = pool.Count;

            // 풀의 모든 객체 파괴
            while (pool.Count > 0)
            {
                var item = pool.Dequeue();
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            objectPools.Remove(type);
            poolPrefabs.Remove(type);

            Log($"Cleared pool for {type.Name}: {count} objects destroyed");
        }
    }

    /// <summary>
    /// 모든 풀 정리
    /// </summary>
    public void ClearAllPools()
    {
        var types = objectPools.Keys.ToArray();

        foreach (var type in types)
        {
            var pool = objectPools[type];
            int count = pool.Count;

            while (pool.Count > 0)
            {
                var item = pool.Dequeue();
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            Log($"Cleared pool for {type.Name}: {count} objects destroyed");
        }

        objectPools.Clear();
        poolPrefabs.Clear();
    }
    #endregion

    #region Events
    public event Action<T> OnItemRegistered;
    public event Action<T> OnItemUnregistered;
    public event Action<T, ItemState, ItemState> OnItemStateChanged;
    #endregion

    #region Basic API (기존 기능 유지)
    public void SetActive(T item, bool active)
    {
        if (item == null)
        {
            LogWarning("Cannot set active state for null item");
            return;
        }

        int instanceId = item.GetInstanceID();

        if (!registeredItems.ContainsKey(instanceId))
        {
            LogWarning($"Item not registered: {item.name} (ID: {instanceId})");
            return;
        }

        item.gameObject.SetActive(active);
        SetItemState(instanceId, active ? ItemState.Active : ItemState.Inactive);
        Log($"Set {item.name} active: {active}");
    }

    public IEnumerable<T> GetAll()
    {
        return registeredItems.Values.Where(item => item != null);
    }

    public TSpecific GetFirst<TSpecific>() where TSpecific : T
    {
        return registeredItems.Values
            .Where(item => item != null && item is TSpecific)
            .Cast<TSpecific>()
            .FirstOrDefault();
    }

    public IEnumerable<TSpecific> GetAll<TSpecific>() where TSpecific : T
    {
        return registeredItems.Values
            .Where(item => item != null && item is TSpecific)
            .Cast<TSpecific>();
    }

    public T GetById(int instanceId)
    {
        registeredItems.TryGetValue(instanceId, out var item);
        return item;
    }

    public int RegisteredCount => registeredItems.Count;
    #endregion

    #region Lifecycle
    protected override void OnDestroy()
    {
        base.OnDestroy();

        // 등록된 객체들 정리
        registeredItems.Clear();

        // 모든 풀 정리
        ClearAllPools();
    }
    #endregion

    #region Debug & Utilities
    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{GetType().Name}] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogWarning($"[{GetType().Name}] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[{GetType().Name}] {message}");
    }

    /// <summary>
    /// 풀링된 객체 수 반환
    /// </summary>
    public int PooledCount
    {
        get
        {
            return objectPools.Values.Sum(pool => pool.Count);
        }
    }

    /// <summary>
    /// 풀 정보 반환
    /// </summary>
    public Dictionary<Type, int> GetPoolInfo()
    {
        return objectPools.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Count
        );
    }

#if UNITY_EDITOR
    [Header("Debug Info")]
    [SerializeField, TextArea(5, 15)] private string debugInfo;

    protected override void OnValidate()
    {
        base.OnValidate();
        if (Application.isPlaying)
        {
            var groupInfo = groups.Count > 0
                ? string.Join("\n", groups.Select(kvp => $"  {kvp.Key}: {kvp.Value.Count}"))
                : "  No groups";

            var tagInfo = tags.Count > 0
                ? string.Join("\n", tags.Select(kvp => $"  {kvp.Key}: {kvp.Value.Count}"))
                : "  No tags";

            var stateInfo = itemStates.GroupBy(kvp => kvp.Value)
                .Select(g => $"  {g.Key}: {g.Count()}")
                .DefaultIfEmpty("  No states")
                .Aggregate((a, b) => a + "\n" + b);

            debugInfo = $"Registered: {RegisteredCount}\n" +
                       $"Groups ({groups.Count}):\n{groupInfo}\n" +
                       $"Tags ({tags.Count}):\n{tagInfo}\n" +
                       $"States:\n{stateInfo}\n" +
                       $"Dependencies: {dependencies.Count}\n" +
                       $"Pooled: {PooledCount}";
        }
    }
#endif
    #endregion
}

/// <summary>
/// ContainerManager의 AggregateRoot 통합을 위한 확장 메서드들
/// </summary>
public static class ContainerManagerExtensions
{
    /// <summary>
    /// ObjectTag enum을 사용한 검색
    /// </summary>
    public static IEnumerable<T> GetByTag<T>(this ContainerManager<T> manager, ObjectTag tag)
        where T : AggregateRoot
    {
        return manager.GetByTag(tag.ToString());
    }

    /// <summary>
    /// 여러 ObjectTag로 검색 (AND 조건)
    /// </summary>
    public static IEnumerable<T> GetByTags<T>(this ContainerManager<T> manager, params ObjectTag[] tags)
        where T : AggregateRoot
    {
        return manager.GetByTags(tags.Select(t => t.ToString()).ToArray());
    }

    /// <summary>
    /// 복합 조건 검색 (그룹 + 태그)
    /// </summary>
    public static IEnumerable<T> GetByGroupAndTag<T>(this ContainerManager<T> manager, string groupName, ObjectTag tag)
        where T : AggregateRoot
    {
        var groupItems = manager.GetGroup(groupName);
        var tagItems = manager.GetByTag(tag);
        return groupItems.Intersect(tagItems);
    }

    /// <summary>
    /// 상태별 ObjectTag 검색
    /// </summary>
    public static IEnumerable<T> GetByTagAndState<T>(this ContainerManager<T> manager, ObjectTag tag, ItemState state)
        where T : AggregateRoot
    {
        return manager.GetByTag(tag).Where(item =>
            manager.GetItemState(item.GetInstanceID()) == state);
    }

    /// <summary>
    /// 상세 리포트 생성
    /// </summary>
    public static string GenerateDetailedReport<T>(this ContainerManager<T> manager)
        where T : AggregateRoot
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine($"=== {typeof(T).Name} Container Report ===");
        report.AppendLine($"Total Registered: {manager.RegisteredCount}");
        report.AppendLine();

        // 상태별 분포
        var stateGroups = manager.GetAll()
            .GroupBy(item => manager.GetItemState(item.GetInstanceID()))
            .OrderBy(g => g.Key);

        report.AppendLine("State Distribution:");
        foreach (var group in stateGroups)
        {
            report.AppendLine($"  {group.Key}: {group.Count()}");
        }
        report.AppendLine();

        // 태그별 분포 (AggregateRoot 기준)
        var tagDistribution = new Dictionary<ObjectTag, int>();
        foreach (var item in manager.GetAll())
        {
            foreach (var tag in item.Tags)
            {
                tagDistribution[tag] = tagDistribution.GetValueOrDefault(tag) + 1;
            }
        }

        if (tagDistribution.Count > 0)
        {
            report.AppendLine("Tag Distribution:");
            foreach (var kvp in tagDistribution.OrderByDescending(x => x.Value))
            {
                report.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
            report.AppendLine();
        }

        // 그룹별 분포 (AggregateRoot 기준)
        var groupDistribution = new Dictionary<string, int>();
        foreach (var item in manager.GetAll())
        {
            foreach (var group in item.Groups)
            {
                groupDistribution[group] = groupDistribution.GetValueOrDefault(group) + 1;
            }
        }

        if (groupDistribution.Count > 0)
        {
            report.AppendLine("Group Distribution:");
            foreach (var kvp in groupDistribution.OrderByDescending(x => x.Value))
            {
                report.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        return report.ToString();
    }
}

/// <summary>
/// 성능 최적화된 쿼리 헬퍼 클래스
/// </summary>
public static class ContainerQueryOptimizer
{
    // 캐시된 결과를 저장 (프레임 단위 캐싱)
    private static readonly Dictionary<string, (int frame, object result)> queryCache = new();

    /// <summary>
    /// 캐시된 그룹 검색 (같은 프레임 내에서는 캐시 사용)
    /// </summary>
    public static IEnumerable<T> GetGroupCached<T>(this ContainerManager<T> manager, string groupName)
        where T : AggregateRoot
    {
        string cacheKey = $"{typeof(T).Name}_Group_{groupName}";
        int currentFrame = Time.frameCount;

        if (queryCache.TryGetValue(cacheKey, out var cached) && cached.frame == currentFrame)
        {
            return (IEnumerable<T>)cached.result;
        }

        var result = manager.GetGroup(groupName).ToList(); // 리스트로 구체화하여 캐싱
        queryCache[cacheKey] = (currentFrame, result);

        return result;
    }

    /// <summary>
    /// 캐시된 태그 검색
    /// </summary>
    public static IEnumerable<T> GetByTagCached<T>(this ContainerManager<T> manager, ObjectTag tag)
        where T : AggregateRoot
    {
        string cacheKey = $"{typeof(T).Name}_Tag_{tag}";
        int currentFrame = Time.frameCount;

        if (queryCache.TryGetValue(cacheKey, out var cached) && cached.frame == currentFrame)
        {
            return (IEnumerable<T>)cached.result;
        }

        var result = manager.GetByTag(tag).ToList();
        queryCache[cacheKey] = (currentFrame, result);

        return result;
    }

    /// <summary>
    /// 캐시 정리 (메모리 정리용)
    /// </summary>
    public static void ClearCache()
    {
        queryCache.Clear();
    }

    /// <summary>
    /// 오래된 캐시 정리 (10프레임 이상 지난 항목)
    /// </summary>
    public static void CleanupOldCache()
    {
        int currentFrame = Time.frameCount;
        var keysToRemove = queryCache.Where(kvp => currentFrame - kvp.Value.frame > 10)
                                   .Select(kvp => kvp.Key)
                                   .ToList();

        foreach (var key in keysToRemove)
        {
            queryCache.Remove(key);
        }
    }
}

/// <summary>
/// 조건부 배치 작업을 위한 빌더 클래스
/// </summary>
public class BatchOperationBuilder<T> where T : AggregateRoot
{
    private readonly ContainerManager<T> manager;
    private readonly List<Func<T, bool>> conditions = new();

    public BatchOperationBuilder(ContainerManager<T> manager)
    {
        this.manager = manager;
    }

    /// <summary>
    /// 그룹 조건 추가
    /// </summary>
    public BatchOperationBuilder<T> InGroup(string groupName)
    {
        conditions.Add(item => item.IsInGroup(groupName));
        return this;
    }

    /// <summary>
    /// 태그 조건 추가
    /// </summary>
    public BatchOperationBuilder<T> WithTag(ObjectTag tag)
    {
        conditions.Add(item => item.HasTag(tag));
        return this;
    }

    /// <summary>
    /// 여러 태그 중 하나라도 가진 경우
    /// </summary>
    public BatchOperationBuilder<T> WithAnyTag(params ObjectTag[] tags)
    {
        conditions.Add(item => item.HasAnyTag(tags));
        return this;
    }

    /// <summary>
    /// 모든 태그를 가진 경우
    /// </summary>
    public BatchOperationBuilder<T> WithAllTags(params ObjectTag[] tags)
    {
        conditions.Add(item => item.HasAllTags(tags));
        return this;
    }

    /// <summary>
    /// 상태 조건 추가
    /// </summary>
    public BatchOperationBuilder<T> InState(ItemState state)
    {
        conditions.Add(item => manager.GetItemState(item.GetInstanceID()) == state);
        return this;
    }

    /// <summary>
    /// 커스텀 조건 추가
    /// </summary>
    public BatchOperationBuilder<T> Where(Func<T, bool> condition)
    {
        conditions.Add(condition);
        return this;
    }

    /// <summary>
    /// 메타데이터 조건 추가
    /// </summary>
    public BatchOperationBuilder<T> WithMetadata(string key, object value)
    {
        conditions.Add(item =>
        {
            var metadata = item.GetMetadata<object>(key);
            return metadata != null && metadata.Equals(value);
        });
        return this;
    }

    /// <summary>
    /// 생성 시간 조건 추가
    /// </summary>
    public BatchOperationBuilder<T> CreatedAfter(DateTime time)
    {
        conditions.Add(item => item.CreatedTime > time);
        return this;
    }

    /// <summary>
    /// 생성 시간 조건 추가
    /// </summary>
    public BatchOperationBuilder<T> CreatedBefore(DateTime time)
    {
        conditions.Add(item => item.CreatedTime < time);
        return this;
    }

    /// <summary>
    /// 조건에 맞는 모든 항목 반환
    /// </summary>
    public IEnumerable<T> Get()
    {
        return manager.GetAll().Where(item => conditions.All(condition => condition(item)));
    }

    /// <summary>
    /// 조건에 맞는 항목 수 반환
    /// </summary>
    public int Count()
    {
        return Get().Count();
    }

    /// <summary>
    /// 조건에 맞는 첫 번째 항목 반환
    /// </summary>
    public T GetFirst()
    {
        return Get().FirstOrDefault();
    }

    /// <summary>
    /// 배치 활성화/비활성화
    /// </summary>
    public int SetActive(bool active)
    {
        var items = Get().ToList();
        foreach (var item in items)
        {
            manager.SetActive(item, active);
        }
        return items.Count;
    }

    /// <summary>
    /// 배치 상태 변경
    /// </summary>
    public int SetState(ItemState state)
    {
        var items = Get().ToList();
        foreach (var item in items)
        {
            manager.SetItemState(item.GetInstanceID(), state);
        }
        return items.Count;
    }

    /// <summary>
    /// 배치 삭제
    /// </summary>
    public int Destroy(bool forceDependencyBreak = false)
    {
        var items = Get().ToList();

        if (forceDependencyBreak)
        {
            foreach (var item in items)
            {
                manager.RemoveAllDependencies(item.GetInstanceID());
            }
        }

        int destroyedCount = 0;
        foreach (var item in items)
        {
            if (manager.CanSafelyUnregister(item.GetInstanceID()))
            {
                UnityEngine.Object.Destroy(item.gameObject);
                destroyedCount++;
            }
        }

        return destroyedCount;
    }

    /// <summary>
    /// 배치 그룹 추가
    /// </summary>
    public int AddToGroup(string groupName)
    {
        var items = Get().ToList();
        foreach (var item in items)
        {
            item.AddToGroup(groupName);
        }
        return items.Count;
    }

    /// <summary>
    /// 배치 그룹 제거
    /// </summary>
    public int RemoveFromGroup(string groupName)
    {
        var items = Get().ToList();
        foreach (var item in items)
        {
            item.RemoveFromGroup(groupName);
        }
        return items.Count;
    }

    /// <summary>
    /// 배치 태그 추가
    /// </summary>
    public int AddTag(ObjectTag tag)
    {
        var items = Get().ToList();
        foreach (var item in items)
        {
            item.AddTag(tag);
        }
        return items.Count;
    }

    /// <summary>
    /// 배치 태그 제거
    /// </summary>
    public int RemoveTag(ObjectTag tag)
    {
        var items = Get().ToList();
        foreach (var item in items)
        {
            item.RemoveTag(tag);
        }
        return items.Count;
    }

    /// <summary>
    /// 배치 메타데이터 설정
    /// </summary>
    public int SetMetadata<TValue>(string key, TValue value)
    {
        var items = Get().ToList();
        foreach (var item in items)
        {
            item.SetMetadata(key, value);
        }
        return items.Count;
    }
}

/// <summary>
/// ContainerManager에 배치 작업 빌더 기능 추가
/// </summary>
public static class BatchOperationExtensions
{
    /// <summary>
    /// 배치 작업 빌더 시작
    /// </summary>
    public static BatchOperationBuilder<T> Batch<T>(this ContainerManager<T> manager)
        where T : AggregateRoot
    {
        return new BatchOperationBuilder<T>(manager);
    }
}

/// <summary>
/// 고급 분석 및 모니터링 도구
/// </summary>
public static class ContainerAnalytics<T> where T : AggregateRoot
{
    /// <summary>
    /// 메모리 사용량 분석
    /// </summary>
    public static ContainerMemoryReport AnalyzeMemoryUsage(ContainerManager<T> manager)
    {
        var report = new ContainerMemoryReport();

        foreach (var item in manager.GetAll())
        {
            report.TotalObjects++;
            report.TotalGroups += item.Groups.Count;
            report.TotalTags += item.Tags.Count;
            report.TotalMetadata += item.GetAllMetadata().Count;
        }

        // 추정 메모리 사용량 (대략적)
        report.EstimatedMemoryUsage =
            report.TotalObjects * 64 +        // 기본 객체 오버헤드
            report.TotalGroups * 24 +         // 그룹 참조
            report.TotalTags * 8 +            // 태그 enum
            report.TotalMetadata * 32;        // 메타데이터 키-값

        return report;
    }

    /// <summary>
    /// 성능 지표 수집
    /// </summary>
    public static ContainerPerformanceMetrics CollectPerformanceMetrics(ContainerManager<T> manager)
    {
        var metrics = new ContainerPerformanceMetrics();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 검색 성능 테스트
        stopwatch.Restart();
        var allItems = manager.GetAll().ToList();
        metrics.GetAllTime = stopwatch.ElapsedMilliseconds;

        // 그룹 검색 성능 (첫 번째 그룹 기준)
        var firstGroup = allItems.FirstOrDefault()?.Groups.FirstOrDefault();
        if (firstGroup != null)
        {
            stopwatch.Restart();
            manager.GetGroup(firstGroup).ToList();
            metrics.GroupSearchTime = stopwatch.ElapsedMilliseconds;
        }

        // 태그 검색 성능 (첫 번째 태그 기준)
        var firstTag = allItems.FirstOrDefault()?.Tags.FirstOrDefault();
        if (firstTag.HasValue)
        {
            stopwatch.Restart();
            manager.GetByTag(firstTag.Value).ToList();
            metrics.TagSearchTime = stopwatch.ElapsedMilliseconds;
        }

        metrics.TotalItems = allItems.Count;
        return metrics;
    }
}

/// <summary>
/// 메모리 사용량 보고서
/// </summary>
public class ContainerMemoryReport
{
    public int TotalObjects;
    public int TotalGroups;
    public int TotalTags;
    public int TotalMetadata;
    public long EstimatedMemoryUsage; // bytes

    public override string ToString()
    {
        return $"Container Memory Report:\n" +
               $"  Objects: {TotalObjects:N0}\n" +
               $"  Groups: {TotalGroups:N0}\n" +
               $"  Tags: {TotalTags:N0}\n" +
               $"  Metadata: {TotalMetadata:N0}\n" +
               $"  Est. Memory: {EstimatedMemoryUsage / 1024.0:F1} KB";
    }
}

/// <summary>
/// 성능 지표 클래스
/// </summary>
public class ContainerPerformanceMetrics
{
    public int TotalItems;
    public long GetAllTime;      // ms
    public long GroupSearchTime; // ms
    public long TagSearchTime;   // ms

    public override string ToString()
    {
        return $"Container Performance Metrics:\n" +
               $"  Total Items: {TotalItems:N0}\n" +
               $"  GetAll Time: {GetAllTime}ms\n" +
               $"  Group Search: {GroupSearchTime}ms\n" +
               $"  Tag Search: {TagSearchTime}ms";
    }
}

// 지원 클래스들
[Serializable]
public class ContainerItemOptions
{
    public List<string> Groups = new();
    public List<string> Tags = new();
    public List<int> Dependencies = new();
}

[Serializable]
public class ContainerItemInfo
{
    public int InstanceId;
    public float RegisterTime;
    public Scene Scene;
    public string OriginalName;
    public Type Type;

    // AggregateRoot 전용 필드들
    public string AggregateId;
    public DateTime CreatedTime;
    public string CreatedScene;
    public Dictionary<string, object> CustomMetadata;

    /// <summary>
    /// 정보를 포맷된 문자열로 변환
    /// </summary>
    public string ToFormattedString()
    {
        return $"Item Info:\n" +
               $"  InstanceID: {InstanceId}\n" +
               $"  AggregateID: {AggregateId ?? "N/A"}\n" +
               $"  Type: {Type?.Name ?? "Unknown"}\n" +
               $"  Name: {OriginalName}\n" +
               $"  Scene: {Scene.name}\n" +
               $"  Registered: {RegisterTime:F2}s\n" +
               $"  Created: {CreatedTime:yyyy-MM-dd HH:mm:ss}\n" +
               $"  Metadata: {CustomMetadata?.Count ?? 0} items";
    }
}

public enum ItemState
{
    Unknown,
    Active,
    Inactive,
    Paused,
    Loading,
    Error,
    Destroyed
}