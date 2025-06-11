using System;
using System.Collections.Generic;

public sealed class RxVar<T> : RxBase, IRxReadable<T>
{
    private T value;
    private readonly List<Action<T>> listeners = new();

    public RxVar(T initialValue = default, IRxOwner owner = null)
    {
        value = initialValue;
        owner?.RegisterRx(this);
    }

    public T Value => value;

    public void SetValue(T newValue, IRxCaller caller) // 값 설정
    {
        if (!caller.IsMultiRolesCaller)
            throw new InvalidOperationException($"An invalid caller({caller}) has accessed.");

        if (!EqualityComparer<T>.Default.Equals(value, newValue))
        {
            value = newValue;
            NotifyAll();
        }
    }

    public void Set(T newValue) // 값 설정
    {
        if (!EqualityComparer<T>.Default.Equals(value, newValue))
        {
            value = newValue;
            NotifyAll();
        }
    }

    public void AddListener(Action<T> listener) // 값 변경을 구독할 수 있음
    {
        if (listener != null)
        {
            listeners.Add(listener);
            listener(value);
        }
    }

    public void RemoveListener(Action<T> listener) // 구독 해제
    {
        if (listener != null)
        {
            listeners.Remove(listener);
        }
    }

    public override void ClearRelation()
    {
        listeners.Clear();
    }

    private void NotifyAll()
    {
        foreach (var listener in listeners)
            listener(value);
    }
}