using System;
using System.Collections.Generic;

namespace Akasha
{
    public class RxVar<T> : IRxField, IRxReadable<T>, IRxWritable<T>, IRxSubscribable<T>
    {
        private T _value;
        private readonly RxSubscription<T> _subscription = new();
        private readonly object _owner;

        public RxVar(T initialValue = default, object owner = null)
        {
            _value = initialValue;
            _owner = owner;
        }

        public T Value => _value;
        public void SetValue(T newValue, object caller)
        {
            if (!IsAuthorized(caller))
                throw new InvalidOperationException($"[RxVar.SetValue] {caller?.GetType().Name}는 RxVar의 값을 변경할 권한이 없습니다.");

            if (!EqualityComparer<T>.Default.Equals(_value, newValue))
            {
                _value = newValue;

                // 지연 실행 시점에서도 Context 보존
                RxQueue.Enqueue(() =>
                {
                    this.WithContext(() =>
                    {
                        _subscription.NotifyAll(_value);
                    });
                }, this);
            }
        }

        private bool IsAuthorized(object caller)
        {
            return caller == _owner || caller is IRxModelOwner;
        }

        public void SubscribeLaw(Action<T> subscriber, object context, RxType relationType)
        {
            if (relationType != RxType.Logical && relationType != RxType.Functional)
                throw new InvalidOperationException("[RxVar.Subscribe] Functional 또는 Logical 구독만 허용됩니다.");

            RxValidator.ValidateSubscriberContext(context, _owner);
            _subscription.Add(subscriber, context, relationType);
        }

        public IDisposable Bind(Action<T> subscriber, object context, RxType relationType)
        {
            SubscribeLaw(subscriber, context, relationType);
            return new SubscriptionDisposable<T>(this, subscriber);
        }

        public void UnsubscribeLaw(Action<T> subscriber) => _subscription.Remove(subscriber);
    }

}
