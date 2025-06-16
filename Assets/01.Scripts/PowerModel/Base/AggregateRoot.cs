using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 모든 관리 대상 객체의 기본 클래스
/// ContainerManager와 완전 통합된 메타데이터 관리 시스템
/// </summary>
public class AggregateRoot : MonoBehaviour
{
    [Header("Object Identity")]
    [SerializeField] private string customAggregateId = "";
    [SerializeField] private bool autoRegister = true;

    [Header("Initial Setup")]
    [SerializeField] private List<string> initialGroups = new();
    [SerializeField] private List<ObjectTag> initialTags = new();

    // 내부 데이터 (ContainerManager와 동기화됨)
    private readonly HashSet<string> groups = new();
    private readonly HashSet<ObjectTag> tags = new();
    private readonly Dictionary<string, object> metadata = new();

    // 상태 정보
    private ObjectState currentState = ObjectState.Created;
    private DateTime createdTime;
    private string createdScene;
    private bool isRegisteredToManager = false;

    #region Properties
    public string AggregateId => string.IsNullOrEmpty(customAggregateId)
        ? $"{GetType().Name}_{GetInstanceID()}"
        : customAggregateId;

    public string TypedId => $"{GetType().Name}_{GetInstanceID()}";
    public ObjectState State => currentState;
    public DateTime CreatedTime => createdTime;
    public string CreatedScene => createdScene;
    public bool IsRegistered => isRegisteredToManager;

    public IReadOnlyCollection<string> Groups => groups;
    public IReadOnlyCollection<ObjectTag> Tags => tags;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        InitializeMetadata();
        OnAwakeComplete();
    }

    protected virtual void Start()
    {
        ChangeState(ObjectState.Initialized);

        if (autoRegister && !isRegisteredToManager)
        {
            AutoRegisterToManager();
        }

        ChangeState(ObjectState.Active);
        OnStartComplete();
    }

    protected virtual void OnDestroy()
    {
        ChangeState(ObjectState.Destroyed);

        if (isRegisteredToManager)
        {
            AutoUnregisterFromManager();
        }

        OnDestroyComplete();
    }

    protected virtual void OnEnable()
    {
        if (currentState == ObjectState.Inactive)
        {
            ChangeState(ObjectState.Active);
        }
        OnEnableComplete();
    }

    protected virtual void OnDisable()
    {
        if (currentState == ObjectState.Active)
        {
            ChangeState(ObjectState.Inactive);
        }
        OnDisableComplete();
    }
    #endregion

    #region Lifecycle Hooks
    protected virtual void OnAwakeComplete() { }
    protected virtual void OnStartComplete() { }
    protected virtual void OnEnableComplete() { }
    protected virtual void OnDisableComplete() { }
    protected virtual void OnDestroyComplete() { }
    protected virtual void OnStateChanged(ObjectState oldState, ObjectState newState) { }
    #endregion

    #region Initialization
    private void InitializeMetadata()
    {
        createdTime = DateTime.Now;
        createdScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        ApplyInitialSettings();
        SetupAutoMetadata();
    }

    private void ApplyInitialSettings()
    {
        foreach (var group in initialGroups.Where(g => !string.IsNullOrEmpty(g)))
        {
            groups.Add(group);
        }

        foreach (var tag in initialTags)
        {
            tags.Add(tag);
        }
    }

    private void SetupAutoMetadata()
    {
        SetMetadata("CreatedTime", createdTime);
        SetMetadata("CreatedScene", createdScene);
        SetMetadata("InitialPosition", transform.position);
        SetMetadata("GameObjectName", gameObject.name);
        SetMetadata("TypeName", GetType().Name);
        SetMetadata("InstanceID", GetInstanceID());
    }
    #endregion

    #region Auto Registration
    private void AutoRegisterToManager()
    {
        try
        {
            bool registered = false;

            if (this is Presenter presenter && GameManager.UI != null)
            {
                GameManager.UI.RegisterWithAutoMetadata(presenter);
                registered = true;
            }
            else if (this is Controller controller && GameManager.World != null)
            {
                GameManager.World.RegisterWithAutoMetadata(controller);
                registered = true;
            }

            isRegisteredToManager = registered;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{AggregateId}] Auto-registration failed: {ex.Message}");
        }
    }

    private void AutoUnregisterFromManager()
    {
        try
        {
            if (this is Presenter presenter && GameManager.UI != null)
            {
                GameManager.UI.Unregister(presenter);
            }
            else if (this is Controller controller && GameManager.World != null)
            {
                GameManager.World.Unregister(controller);
            }

            isRegisteredToManager = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{AggregateId}] Auto-unregistration failed: {ex.Message}");
        }
    }
    #endregion

    #region Group Management
    public bool AddToGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName)) return false;

        bool added = groups.Add(groupName);
        if (added)
        {
            NotifyManagerGroupChange(groupName, true);
        }
        return added;
    }

    public bool RemoveFromGroup(string groupName)
    {
        bool removed = groups.Remove(groupName);
        if (removed)
        {
            NotifyManagerGroupChange(groupName, false);
        }
        return removed;
    }

    public bool IsInGroup(string groupName) => groups.Contains(groupName);
    public bool IsInAnyGroup(params string[] groupNames) => groupNames.Any(groups.Contains);
    public bool IsInAllGroups(params string[] groupNames) => groupNames.All(groups.Contains);

    public void ClearGroups()
    {
        var oldGroups = new HashSet<string>(groups);
        groups.Clear();

        foreach (var group in oldGroups)
        {
            NotifyManagerGroupChange(group, false);
        }
    }
    #endregion

    #region Tag Management
    public bool AddTag(ObjectTag tag)
    {
        bool added = tags.Add(tag);
        if (added)
        {
            NotifyManagerTagChange(tag, true);
        }
        return added;
    }

    public bool RemoveTag(ObjectTag tag)
    {
        bool removed = tags.Remove(tag);
        if (removed)
        {
            NotifyManagerTagChange(tag, false);
        }
        return removed;
    }

    public bool HasTag(ObjectTag tag) => tags.Contains(tag);
    public bool HasAnyTag(params ObjectTag[] tagsToCheck) => tagsToCheck.Any(tags.Contains);
    public bool HasAllTags(params ObjectTag[] tagsToCheck) => tagsToCheck.All(tags.Contains);

    public void ClearTags()
    {
        var oldTags = new HashSet<ObjectTag>(tags);
        tags.Clear();

        foreach (var tag in oldTags)
        {
            NotifyManagerTagChange(tag, false);
        }
    }
    #endregion

    #region Metadata Management
    public void SetMetadata<T>(string key, T value)
    {
        if (string.IsNullOrEmpty(key)) return;
        metadata[key] = value;
    }

    public T GetMetadata<T>(string key, T defaultValue = default(T))
    {
        if (metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    public bool HasMetadata(string key) => metadata.ContainsKey(key);
    public bool RemoveMetadata(string key) => metadata.Remove(key);
    public Dictionary<string, object> GetAllMetadata() => new Dictionary<string, object>(metadata);
    #endregion

    #region State Management
    private void ChangeState(ObjectState newState)
    {
        if (currentState == newState) return;

        var oldState = currentState;
        currentState = newState;

        OnStateChanged(oldState, newState);
        NotifyManagerStateChange(oldState, newState);
    }

    public void ForceChangeState(ObjectState newState)
    {
        ChangeState(newState);
    }
    #endregion

    #region Manager Communication
    internal ContainerItemInfo CreateItemInfo()
    {
        return new ContainerItemInfo
        {
            InstanceId = GetInstanceID(),
            RegisterTime = Time.time,
            Scene = gameObject.scene,
            OriginalName = name,
            Type = GetType(),
            AggregateId = this.AggregateId,
            CreatedTime = this.CreatedTime,
            CreatedScene = this.CreatedScene,
            CustomMetadata = new Dictionary<string, object>(metadata)
        };
    }

    internal string[] GetGroupArray() => groups.ToArray();
    internal string[] GetTagStringArray() => tags.Select(tag => tag.ToString()).ToArray();

    internal void OnManagerSync_GroupAdded(string groupName) => groups.Add(groupName);
    internal void OnManagerSync_GroupRemoved(string groupName) => groups.Remove(groupName);
    internal void OnManagerSync_TagAdded(string tagName)
    {
        if (Enum.TryParse<ObjectTag>(tagName, out var tag))
            tags.Add(tag);
    }
    internal void OnManagerSync_TagRemoved(string tagName)
    {
        if (Enum.TryParse<ObjectTag>(tagName, out var tag))
            tags.Remove(tag);
    }
    #endregion

    #region Manager Notification
    private void NotifyManagerGroupChange(string groupName, bool added)
    {
        if (!isRegisteredToManager) return;

        try
        {
            if (this is Presenter && GameManager.UI != null)
            {
                if (added) GameManager.UI.AddToGroup(GetInstanceID(), groupName);
                else GameManager.UI.RemoveFromGroup(GetInstanceID(), groupName);
            }
            else if (this is Controller && GameManager.World != null)
            {
                if (added) GameManager.World.AddToGroup(GetInstanceID(), groupName);
                else GameManager.World.RemoveFromGroup(GetInstanceID(), groupName);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{AggregateId}] Failed to notify manager of group change: {ex.Message}");
        }
    }

    private void NotifyManagerTagChange(ObjectTag tag, bool added)
    {
        if (!isRegisteredToManager) return;

        try
        {
            string tagString = tag.ToString();

            if (this is Presenter && GameManager.UI != null)
            {
                if (added) GameManager.UI.AddTag(GetInstanceID(), tagString);
                else GameManager.UI.RemoveTag(GetInstanceID(), tagString);
            }
            else if (this is Controller && GameManager.World != null)
            {
                if (added) GameManager.World.AddTag(GetInstanceID(), tagString);
                else GameManager.World.RemoveTag(GetInstanceID(), tagString);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{AggregateId}] Failed to notify manager of tag change: {ex.Message}");
        }
    }

    private void NotifyManagerStateChange(ObjectState oldState, ObjectState newState)
    {
        if (!isRegisteredToManager) return;

        try
        {
            var itemState = MapToItemState(newState);

            if (this is Presenter && GameManager.UI != null)
            {
                GameManager.UI.SetItemState(GetInstanceID(), itemState);
            }
            else if (this is Controller && GameManager.World != null)
            {
                GameManager.World.SetItemState(GetInstanceID(), itemState);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{AggregateId}] Failed to notify manager of state change: {ex.Message}");
        }
    }

    private ItemState MapToItemState(ObjectState objectState)
    {
        return objectState switch
        {
            ObjectState.Created => ItemState.Loading,
            ObjectState.Initialized => ItemState.Loading,
            ObjectState.Active => ItemState.Active,
            ObjectState.Inactive => ItemState.Inactive,
            ObjectState.Destroyed => ItemState.Destroyed,
            _ => ItemState.Unknown
        };
    }
    #endregion

    #region Utility
    public override string ToString()
    {
        return $"{GetType().Name}(ID:{AggregateId}, State:{currentState}, Groups:{groups.Count}, Tags:{tags.Count})";
    }

    public string GetDetailedInfo()
    {
        var groupList = groups.Count > 0 ? string.Join(", ", groups) : "None";
        var tagList = tags.Count > 0 ? string.Join(", ", tags) : "None";

        return $"AggregateRoot Details:\n" +
               $"  ID: {AggregateId}\n" +
               $"  Type: {GetType().Name}\n" +
               $"  State: {currentState}\n" +
               $"  Created: {createdTime:yyyy-MM-dd HH:mm:ss}\n" +
               $"  Scene: {createdScene}\n" +
               $"  Groups: {groupList}\n" +
               $"  Tags: {tagList}\n" +
               $"  Metadata: {metadata.Count} items\n" +
               $"  Registered: {isRegisteredToManager}";
    }
    #endregion
}

// 상태 및 태그 정의
public enum ObjectState
{
    Created,      // 생성됨 (Awake 완료)
    Initialized,  // 초기화 완료 (Start 시작)
    Active,       // 활성 상태 (Start 완료 또는 OnEnable)
    Inactive,     // 비활성 상태 (OnDisable)
    Destroyed     // 파괴됨 (OnDestroy)
}

public enum ObjectTag
{
    // === 공통 태그 ===
    Temporary, Important, Pooled, Debug, System,

    // === UI 태그 ===
    UI, Menu, HUD, Dialog, Button, Panel,

    // === 게임플레이 태그 ===
    Player, Enemy, Boss, NPC, Item, Weapon, Projectile,

    // === 이동 방식 태그 ===
    Flying, Ground, Underwater,

    // === 상태 태그 ===
    Invincible, Hidden, Frozen, Burning,

    // === 레벨/구역 태그 ===
    Tutorial, MainGame, Cutscene, Loading,

    // === 성능 태그 ===
    HighPriority, LowPriority, Optimized,

    // === 네트워크 태그 ===
    Networked, Local, Synced
}