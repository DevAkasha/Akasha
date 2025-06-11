
using System;

namespace Akasha
{
    public class RxTrigger : IRxEvent
    {
        private readonly RxSubscription _subscription = new();

        public void Raise()
        {
            RxQueue.Enqueue(() =>
            {
                this.WithContext(() => _subscription.NotifyAll());
            }, this);
        }

        public void Subscribe(Action subscriber, object context, RxType relationType)
        {
            if (relationType != RxType.Logical)
                throw new InvalidOperationException("[RxTrigger] Trigger는 Logical 구독만 허용됩니다.");

            RxValidator.ValidateSubscriberContext(context, this);
            _subscription.Add(subscriber, context, relationType);
        }
        public void Unsubscribe(Action subscriber, object context)
        {
            _subscription.Remove(_ => subscriber(), context);
        }

        public void Unsubscribe(Action subscriber) => _subscription.Remove(subscriber);

        public IDisposable Bind(Action subscriber, object context) => RxEventDisposable.Create(this, subscriber, context);
    }

    public class RxLocalEvent : IRxEvent
    {
        private readonly RxSubscription _subscription = new();

        public void Raise()
        {
            RxQueue.Enqueue(() =>
            {
                this.WithContext(() => _subscription.NotifyAll());
            }, this);
        }

        public void Subscribe(Action subscriber, object context, RxType relationType)
        {
            if (relationType != RxType.Logical)
                throw new InvalidOperationException("[RxLocalEvent] LocalEvent는 Logical 구독만 허용됩니다.");

            RxValidator.ValidateSubscriberContext(context, this);
            _subscription.Add(subscriber, context, relationType);
        }

        public void Unsubscribe(Action subscriber) => _subscription.Remove(subscriber);
        public void Unsubscribe(Action subscriber, object context)
        {
            _subscription.Remove(_ => subscriber(), context);
        }

        public IDisposable Bind(Action subscriber, object context) => RxEventDisposable.Create(this, subscriber, context);

    }

    public class RxGlobalEvent : IRxEvent
    {
        private readonly RxSubscription _subscription = new();

        public void Raise()
        {
            RxQueue.Enqueue(() =>
            {
                this.WithContext(() => _subscription.NotifyAll());
            }, this);
        }

        public void Subscribe(Action subscriber, object context, RxType relationType)
        {
            if (relationType != RxType.Logical)
                throw new InvalidOperationException("[RxGlobalEvent] GlobalEvent는 Logical 구독만 허용됩니다.");

            RxValidator.ValidateSubscriberContext(context, this);
            _subscription.Add(subscriber, context, relationType);
        }

        public void Unsubscribe(Action subscriber) => _subscription.Remove(subscriber);
        public void Unsubscribe(Action subscriber, object context)
        {
            _subscription.Remove(_ => subscriber(), context);
        }

        public IDisposable Bind(Action subscriber, object context) => RxEventDisposable.Create(this, subscriber, context);
    }

}
