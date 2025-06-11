using System;

namespace Akasha
{
    public class RxFlag : IRxField, IRxReadable<bool>, IRxSubscribable<bool>
    {
        private bool _value;
        private readonly IRxObservable<bool> _expression;
        private readonly RxSubscription<bool> _subscription = new();

        public bool Value => _value;

        public object? ReactiveOwner { get; private set; }

        public void SetReactiveOwner(object owner)
        {
            ReactiveOwner = owner;
        }

        public RxFlag(IRxObservable<bool> expression, object owner)
        {
            if (!IsValidOwner(owner))
                throw new InvalidOperationException($"[RxFlag.ctor] {owner?.GetType().Name}는 RxFlag의 유효한 소유자가 아닙니다.");

            _expression = expression;
            _value = this.WithContext(() => _expression.Value);
            _expression.SubscribeLaw(OnExprChanged, this, RxType.Functional);
        }

        private static bool IsValidOwner(object owner)
        {
            return owner is IRxFlagger || owner is IScreen || owner is IRxUnsafe;
        }

        private void OnExprChanged(bool newValue)
        {
            if (_value != newValue)
            {
                _value = newValue;
                RxQueue.Enqueue(() => _subscription.NotifyAll(_value), this);
            }
        }

        public void SubscribeLaw(Action<bool> subscriber, object context, RxType relationType)
        {
            if (relationType != RxType.Functional && relationType != RxType.Logical)
                throw new InvalidOperationException("[RxFlag.Subscribe] Functional 또는 Logical 구독만 허용됩니다.");

            RxValidator.ValidateSubscriberContext(context, this);
            _subscription.Add(subscriber, context, relationType);
        }


        public IDisposable Bind(Action<bool> subscriber, object context, RxType relationType)
        {
            SubscribeLaw(subscriber, context, relationType);
            return new SubscriptionDisposable<bool>(this, subscriber);
        }

        public void UnsubscribeLaw(Action<bool> subscriber) => _subscription.Remove(subscriber);
    }
}