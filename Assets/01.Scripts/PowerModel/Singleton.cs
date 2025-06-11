using System.Collections.Generic;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour,IRxOwner,IRxCaller where T : Singleton<T>
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
            }
            return instance;
        }
    }
    public static bool IsInstance => instance != null;
    
    protected virtual bool IsPersistent => true;

    protected virtual void Awake()
    {
        if (instance == null || instance == this)
        {
            instance = (T)this;

            if (IsPersistent)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        Unload();    
    }

    public bool IsRxVarOwner => true;
    public bool IsRxAllOwner => false;
    public bool IsLogicalCaller => true;
    public bool IsMultiRolesCaller => true;
    public bool IsFunctionalCaller => true;

    private readonly HashSet<RxBase> trackedRxVars = new();

    public void RegisterRx(RxBase rx)
    {
        trackedRxVars.Add(rx);
    }

    public void Unload()
    {
        foreach (var rx in trackedRxVars)
        {
            rx.ClearRelation();
        }
        trackedRxVars.Clear();
    }
}

