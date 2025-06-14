using System;
using System.Collections.Generic;
using UnityEngine;

#region Slot 기본 인터페이스 및 베이스 클래스
public interface IViewSlot
{
    void SetOwner(BaseView owner);
    void SetFieldName(string fieldName);
    void Cleanup();
    Type FieldType { get; }
    Type ValueType { get; }
    object GetCurrentValue();
}

public abstract class BaseViewSlot<TRxField, TValue> : IViewSlot
    where TRxField : RxBase
{
    protected BaseView owner;
    protected string fieldName;
    protected TRxField boundRxField;
    protected readonly List<Action<object>> valueChangeListeners = new();

    public Type FieldType => typeof(TRxField);
    public Type ValueType => typeof(TValue);
    public string FieldName => fieldName;
    public bool IsBound => boundRxField != null;

    public void SetOwner(BaseView view)
    {
        owner = view;
    }

    public void SetFieldName(string name)
    {
        fieldName = name;
    }

    public void BindToField(TRxField rxField)
    {
        if (boundRxField != null)
        {
            UnbindFromField();
        }

        boundRxField = rxField;
        if (boundRxField != null)
        {
            SubscribeToField();
            NotifyCurrentValue();

            Debug.Log($"[{GetType().Name}] Bound to RxField: {fieldName}");
        }
    }

    protected virtual void UnbindFromField()
    {
        if (boundRxField != null)
        {
            UnsubscribeFromField();
            boundRxField = null;

            Debug.Log($"[{GetType().Name}] Unbound from RxField: {fieldName}");
        }
    }

    protected abstract void SubscribeToField();
    protected abstract void UnsubscribeFromField();
    protected abstract void NotifyCurrentValue();

    protected void NotifyValueChanged(TValue newValue)
    {
        foreach (var listener in valueChangeListeners)
        {
            try
            {
                listener?.Invoke(newValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error in value change listener: {ex.Message}");
            }
        }
    }

    public void AddValueChangeListener(Action<object> listener)
    {
        if (listener != null && !valueChangeListeners.Contains(listener))
        {
            valueChangeListeners.Add(listener);
        }
    }

    public void RemoveValueChangeListener(Action<object> listener)
    {
        valueChangeListeners.Remove(listener);
    }

    public abstract object GetCurrentValue();
    public abstract TValue Value { get; }

    public virtual void Cleanup()
    {
        UnbindFromField();
        valueChangeListeners.Clear();

        Debug.Log($"[{GetType().Name}] ViewSlot cleaned up");
    }
}
#endregion

#region 제네릭 Slot 구현체들
/// <summary>
/// RxVar<T>를 위한 제네릭 슬롯
/// 사용 예: RxVarSlot<int>, RxVarSlot<float>, RxVarSlot<string>
/// </summary>
public class RxVarSlot<T> : BaseViewSlot<RxVar<T>, T>
{
    private Action<T> valueCallback;

    protected override void SubscribeToField()
    {
        if (boundRxField != null)
        {
            valueCallback = OnValueChanged;
            boundRxField.AddListener(valueCallback);
        }
    }

    protected override void UnsubscribeFromField()
    {
        if (boundRxField != null && valueCallback != null)
        {
            boundRxField.RemoveListener(valueCallback);
            valueCallback = null;
        }
    }

    protected override void NotifyCurrentValue()
    {
        if (boundRxField != null)
        {
            OnValueChanged(boundRxField.Value);
        }
    }

    private void OnValueChanged(T newValue)
    {
        NotifyValueChanged(newValue);
    }

    public override object GetCurrentValue()
    {
        return Value;
    }

    public override T Value => boundRxField != null ? boundRxField.Value : default(T);
}

/// <summary>
/// RxModInt를 위한 슬롯
/// </summary>
public class RxModIntSlot : BaseViewSlot<RxModInt, int>
{
    private Action<int> valueCallback;

    protected override void SubscribeToField()
    {
        if (boundRxField != null)
        {
            valueCallback = OnValueChanged;
            boundRxField.AddListener(valueCallback);
        }
    }

    protected override void UnsubscribeFromField()
    {
        if (boundRxField != null && valueCallback != null)
        {
            boundRxField.RemoveListener(valueCallback);
            valueCallback = null;
        }
    }

    protected override void NotifyCurrentValue()
    {
        if (boundRxField != null)
        {
            OnValueChanged(boundRxField.Value);
        }
    }

    private void OnValueChanged(int newValue)
    {
        NotifyValueChanged(newValue);
    }

    public override object GetCurrentValue()
    {
        return Value;
    }

    public override int Value => boundRxField?.Value ?? 0;
}

/// <summary>
/// RxModFloat를 위한 슬롯
/// </summary>
public class RxModFloatSlot : BaseViewSlot<RxModFloat, float>
{
    private Action<float> valueCallback;

    protected override void SubscribeToField()
    {
        if (boundRxField != null)
        {
            valueCallback = OnValueChanged;
            boundRxField.AddListener(valueCallback);
        }
    }

    protected override void UnsubscribeFromField()
    {
        if (boundRxField != null && valueCallback != null)
        {
            boundRxField.RemoveListener(valueCallback);
            valueCallback = null;
        }
    }

    protected override void NotifyCurrentValue()
    {
        if (boundRxField != null)
        {
            OnValueChanged(boundRxField.Value);
        }
    }

    private void OnValueChanged(float newValue)
    {
        NotifyValueChanged(newValue);
    }

    public override object GetCurrentValue()
    {
        return Value;
    }

    public override float Value => boundRxField?.Value ?? 0f;
}

/// <summary>
/// RxModLong를 위한 슬롯
/// </summary>
public class RxModLongSlot : BaseViewSlot<RxModLong, long>
{
    private Action<long> valueCallback;

    protected override void SubscribeToField()
    {
        if (boundRxField != null)
        {
            valueCallback = OnValueChanged;
            boundRxField.AddListener(valueCallback);
        }
    }

    protected override void UnsubscribeFromField()
    {
        if (boundRxField != null && valueCallback != null)
        {
            boundRxField.RemoveListener(valueCallback);
            valueCallback = null;
        }
    }

    protected override void NotifyCurrentValue()
    {
        if (boundRxField != null)
        {
            OnValueChanged(boundRxField.Value);
        }
    }

    private void OnValueChanged(long newValue)
    {
        NotifyValueChanged(newValue);
    }

    public override object GetCurrentValue()
    {
        return Value;
    }

    public override long Value => boundRxField?.Value ?? 0L;
}

/// <summary>
/// RxModDouble를 위한 슬롯
/// </summary>
public class RxModDoubleSlot : BaseViewSlot<RxModDouble, double>
{
    private Action<double> valueCallback;

    protected override void SubscribeToField()
    {
        if (boundRxField != null)
        {
            valueCallback = OnValueChanged;
            boundRxField.AddListener(valueCallback);
        }
    }

    protected override void UnsubscribeFromField()
    {
        if (boundRxField != null && valueCallback != null)
        {
            boundRxField.RemoveListener(valueCallback);
            valueCallback = null;
        }
    }

    protected override void NotifyCurrentValue()
    {
        if (boundRxField != null)
        {
            OnValueChanged(boundRxField.Value);
        }
    }

    private void OnValueChanged(double newValue)
    {
        NotifyValueChanged(newValue);
    }

    public override object GetCurrentValue()
    {
        return Value;
    }

    public override double Value => boundRxField?.Value ?? 0d;
}

/// <summary>
/// FSM<T>를 위한 제네릭 슬롯
/// 사용 예: FSMSlot<PlayerState>, FSMSlot<UIState>
/// </summary>
public class FSMSlot<TState> : BaseViewSlot<FSM<TState>, TState> where TState : Enum
{
    private Action<TState> valueCallback;

    protected override void SubscribeToField()
    {
        if (boundRxField != null)
        {
            valueCallback = OnValueChanged;
            boundRxField.AddListener(valueCallback);
        }
    }

    protected override void UnsubscribeFromField()
    {
        if (boundRxField != null && valueCallback != null)
        {
            boundRxField.RemoveListener(valueCallback);
            valueCallback = null;
        }
    }

    protected override void NotifyCurrentValue()
    {
        if (boundRxField != null)
        {
            OnValueChanged(boundRxField.Value);
        }
    }

    private void OnValueChanged(TState newValue)
    {
        NotifyValueChanged(newValue);
    }

    public override object GetCurrentValue()
    {
        return Value;
    }

    public override TState Value => boundRxField != null ? boundRxField.Value : default(TState);

    // FSM 특화 메서드들
    public bool CanTransitTo(TState state)
    {
        return boundRxField?.CanTransitTo(state) ?? false;
    }

    public bool IsInState(TState state)
    {
        return EqualityComparer<TState>.Default.Equals(Value, state);
    }
}

/// <summary>
/// RxStateFlagSet<T>를 위한 제네릭 슬롯
/// 사용 예: FlagSetSlot<PlayerFlags>, FlagSetSlot<UIFlags>
/// </summary>
public class FlagSetSlot<TFlag> : BaseViewSlot<RxStateFlagSet<TFlag>, RxStateFlagSet<TFlag>> where TFlag : Enum
{
    private readonly Dictionary<TFlag, Action<bool>> flagCallbacks = new();

    protected override void SubscribeToField()
    {
        if (boundRxField != null)
        {
            // 모든 플래그에 대해 개별 구독
            foreach (TFlag flag in Enum.GetValues(typeof(TFlag)))
            {
                Action<bool> callback = (value) => OnFlagChanged(flag, value);
                flagCallbacks[flag] = callback;
                boundRxField.AddListener(flag, callback);
            }
        }
    }

    protected override void UnsubscribeFromField()
    {
        if (boundRxField != null)
        {
            foreach (var pair in flagCallbacks)
            {
                boundRxField.RemoveListener(pair.Key, pair.Value);
            }
            flagCallbacks.Clear();
        }
    }

    protected override void NotifyCurrentValue()
    {
        if (boundRxField != null)
        {
            NotifyValueChanged(boundRxField);
        }
    }

    private void OnFlagChanged(TFlag flag, bool value)
    {
        NotifyValueChanged(boundRxField);
    }

    public override object GetCurrentValue()
    {
        return Value;
    }

    public override RxStateFlagSet<TFlag> Value => boundRxField;

    // FlagSet 특화 메서드들
    public bool GetFlag(TFlag flag)
    {
        return boundRxField?.GetValue(flag) ?? false;
    }

    public bool AnyActive(params TFlag[] flags)
    {
        return boundRxField?.AnyActive(flags) ?? false;
    }

    public bool AllSatisfied(params TFlag[] flags)
    {
        return boundRxField?.AllSatisfied(flags) ?? false;
    }
}
#endregion

#region Slot 팩토리
public static class ViewSlotFactory
{
    public static RxVarSlot<T> CreateRxVar<T>(string fieldName)
    {
        var slot = new RxVarSlot<T>();
        slot.SetFieldName(fieldName);
        return slot;
    }

    public static RxModIntSlot CreateRxModInt(string fieldName)
    {
        var slot = new RxModIntSlot();
        slot.SetFieldName(fieldName);
        return slot;
    }

    public static RxModFloatSlot CreateRxModFloat(string fieldName)
    {
        var slot = new RxModFloatSlot();
        slot.SetFieldName(fieldName);
        return slot;
    }

    public static RxModLongSlot CreateRxModLong(string fieldName)
    {
        var slot = new RxModLongSlot();
        slot.SetFieldName(fieldName);
        return slot;
    }

    public static RxModDoubleSlot CreateRxModDouble(string fieldName)
    {
        var slot = new RxModDoubleSlot();
        slot.SetFieldName(fieldName);
        return slot;
    }

    public static FSMSlot<TState> CreateFSM<TState>(string fieldName) where TState : Enum
    {
        var slot = new FSMSlot<TState>();
        slot.SetFieldName(fieldName);
        return slot;
    }

    public static FlagSetSlot<TFlag> CreateFlagSet<TFlag>(string fieldName) where TFlag : Enum
    {
        var slot = new FlagSetSlot<TFlag>();
        slot.SetFieldName(fieldName);
        return slot;
    }
}
#endregion
