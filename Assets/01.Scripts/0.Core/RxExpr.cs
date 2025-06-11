using System;
using System.Collections.Generic;

namespace Akasha
{
    public class RxExpr<T> : IRxField, IRxReadable<T>, IRxSubscribable<T>
    {
        private T _value;
        private readonly Func<T> _compute;
        private readonly RxSubscription<T> _subscription = new();

        public T Value => _value;

        public RxExpr(Func<T> compute, params IRxObservable<T>[] dependencies)
        {
            _compute = compute ?? throw new ArgumentNullException(nameof(compute));

            foreach (var dep in dependencies)
            {
                dep.SubscribeLaw(_ => Recalculate(), this, RxType.Functional);
            }

            _value = this.WithContext(() => _compute.Invoke());
        }

        private void Recalculate()
        {
            var newValue = this.WithContext(() => _compute.Invoke());
            if (!EqualityComparer<T>.Default.Equals(_value, newValue))
            {
                _value = newValue;
                _subscription.NotifyAll(_value);
            }
        }

        public void SubscribeLaw(Action<T> subscriber, object context, RxType relationType)
        {
            if (relationType != RxType.Functional && relationType != RxType.Logical)
                throw new InvalidOperationException("[RxExpr.Subscribe] Functional 또는 Logical 구독만 허용됩니다.");

            RxValidator.ValidateSubscriberContext(context, this);
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