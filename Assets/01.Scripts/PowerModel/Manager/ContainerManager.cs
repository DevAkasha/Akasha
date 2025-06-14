using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class ContainerManager<T> : ManagerBase where T : AggregateRoot
{
    [Header("Container Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    #region Registry Management
    // InstanceID를 키로 하는 등록된 객체 관리
    private readonly Dictionary<int, T> registeredItems = new();

    /// <summary>
    /// 객체를 관리 대상으로 등록
    /// </summary>
    public void Register(T item)
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
        Log($"Registered: {item.name} (ID: {instanceId})");
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

        if (registeredItems.Remove(instanceId))
        {
            Log($"Unregistered: {item.name} (ID: {instanceId})");
        }
        else
        {
            LogWarning($"Item not found for unregister: {item.name} (ID: {instanceId})");
        }
    }

    /// <summary>
    /// 등록된 객체의 활성화 상태 제어
    /// </summary>
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
        Log($"Set {item.name} active: {active}");
    }

    /// <summary>
    /// 모든 등록된 객체 반환
    /// </summary>
    public IEnumerable<T> GetAll()
    {
        return registeredItems.Values.Where(item => item != null);
    }

    /// <summary>
    /// 특정 타입의 첫 번째 등록된 객체 반환
    /// </summary>
    public TSpecific GetFirst<TSpecific>() where TSpecific : T
    {
        return registeredItems.Values
            .Where(item => item != null && item is TSpecific)
            .Cast<TSpecific>()
            .FirstOrDefault();
    }

    /// <summary>
    /// 특정 타입의 모든 등록된 객체 반환
    /// </summary>
    public IEnumerable<TSpecific> GetAll<TSpecific>() where TSpecific : T
    {
        return registeredItems.Values
            .Where(item => item != null && item is TSpecific)
            .Cast<TSpecific>();
    }

    /// <summary>
    /// InstanceID로 객체 검색
    /// </summary>
    public T GetById(int instanceId)
    {
        registeredItems.TryGetValue(instanceId, out var item);
        return item;
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
    /// <summary>
    /// 등록된 객체 수 반환
    /// </summary>
    public int RegisteredCount => registeredItems.Count;

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

#if UNITY_EDITOR
    [Header("Debug Info")]
    [SerializeField, TextArea(3, 10)] private string debugInfo;

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            var poolInfo = GetPoolInfo();
            var poolText = poolInfo.Count > 0
                ? string.Join("\n", poolInfo.Select(kvp => $"{kvp.Key.Name}: {kvp.Value}"))
                : "No pools";

            debugInfo = $"Registered: {RegisteredCount}\n" +
                       $"Pooled: {PooledCount}\n" +
                       $"Pool Info:\n{poolText}";
        }
    }
#endif
    #endregion
}
