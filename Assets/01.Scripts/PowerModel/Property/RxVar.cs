using System;
using System.Collections.Generic;
using static UnityEngine.UI.GridLayoutGroup;

namespace Akasha
{
    public sealed class RxVar<T> : RxBase, IRxField<T>
    {
        private T value;
        private readonly List<Action<T>> listeners = new();

        public string FieldName { get; set; }
        public T Value => value;

        public RxVar(T initialValue = default, IRxOwner owner = null)
        {
            value = initialValue;
            FieldName = "";
            owner?.RegisterRx(this);
        }

        public RxVar(T initialValue = default, string fieldName = null, IRxOwner owner = null)
        {
            value = initialValue;

            if (!string.IsNullOrEmpty(fieldName))
                FieldName = fieldName;

            owner?.RegisterRx(this);
        }

        public void SetValue(T newValue, IRxCaller caller)
        {
            if (!caller.IsMultiRolesCaller)
                throw new InvalidOperationException($"An invalid caller({caller}) has accessed.");

            if (!EqualityComparer<T>.Default.Equals(value, newValue))
            {
                value = newValue;
                NotifyAll();
            }
        }

        public void Set(T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(value, newValue))
            {
                value = newValue;
                NotifyAll();
            }
        }

        public void AddListener(Action<T> listener)
        {
            if (listener != null)
            {
                listeners.Add(listener);
                listener(value);
            }
        }

        public void RemoveListener(Action<T> listener)
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

        public override bool Satisfies(Func<object, bool> predicate)
            => predicate?.Invoke(Value) ?? false;

        private void NotifyAll()
        {
            foreach (var listener in listeners)
                listener(value);
        }
    }
}