using System;
using System.Collections.Generic;
using System.Linq;

namespace Akasha
{
    public class RxList<T> : IRxField, IRxReadable<List<T>>, IRxSubscribable<List<T>>
    {
        private readonly List<T> _items = new();
        private readonly RxSubscription<List<T>> _subscription = new();
        private readonly RxSubscription<ListDelta<T>> _deltaSubscription = new();

        public List<T> Value => _items.ToList();

        public int Count => _items.Count;
        public T this[int index] => _items[index];

        public void Add(T item)
        {
            _items.Add(item);

            RxQueue.Enqueue(() =>
            {
                Notify();
                NotifyDelta(new ListDelta<T>.Add(item));
            }, this);
        }

        public bool Remove(T item)
        {
            var result = _items.Remove(item);
            if (result)
            {
                Notify();
                NotifyDelta(new ListDelta<T>.Remove(item));
            }
            return result;
        }

        public void Insert(int index, T item)
        {
            _items.Insert(index, item);
            Notify();
            NotifyDelta(new ListDelta<T>.Insert(index, item));
        }

        public void RemoveAt(int index)
        {
            var item = _items[index];
            _items.RemoveAt(index);
            Notify();
            NotifyDelta(new ListDelta<T>.RemoveAt(index, item));
        }

        public void Clear()
        {
            _items.Clear();
            Notify();
            NotifyDelta(new ListDelta<T>.Clear());
        }

        public void Replace(int index, T item)
        {
            var old = _items[index];
            _items[index] = item;
            Notify();
            NotifyDelta(new ListDelta<T>.Replace(index, old, item));
        }

        public void Move(int oldIndex, int newIndex)
        {
            var item = _items[oldIndex];
            _items.RemoveAt(oldIndex);
            _items.Insert(newIndex, item);
            Notify();
            NotifyDelta(new ListDelta<T>.Move(oldIndex, newIndex, item));
        }

        public void ReplaceAll(IEnumerable<T> newItems)
        {
            _items.Clear();
            _items.AddRange(newItems);
            Notify();
            NotifyDelta(new ListDelta<T>.Clear());
        }

        public void Swap(int indexA, int indexB)
        {
            var temp = _items[indexA];
            _items[indexA] = _items[indexB];
            _items[indexB] = temp;
            Notify();
            NotifyDelta(new ListDelta<T>.Replace(indexA, _items[indexB], _items[indexA]));
            NotifyDelta(new ListDelta<T>.Replace(indexB, _items[indexA], _items[indexB]));
        }

        public void Sort(Comparison<T> comparison)
        {
            _items.Sort(comparison);
            Notify();
        }

        private void Notify() => this.WithContext(() => _subscription.NotifyAll(Value));

        private void NotifyDelta(ListDelta<T> delta) => this.WithContext(() => _deltaSubscription.NotifyAll(delta));

        public void SubscribeLaw(Action<List<T>> subscriber, object context, RxType relationType)
        {
            if (relationType != RxType.Logical && relationType != RxType.Functional)
                throw new InvalidOperationException("[RxList.Subscribe] Functional 또는 Logical 구독만 허용됩니다.");

            RxValidator.ValidateSubscriberContext(context, this);
            _subscription.Add(subscriber, context, relationType);
        }

        public void SubscribeDelta(Action<ListDelta<T>> subscriber, object context, RxType relationType)
        {
            if (relationType != RxType.Logical)
                throw new InvalidOperationException("[RxList.SubscribeDelta] Logical 구독만 허용됩니다.");

            RxValidator.ValidateSubscriberContext(context, this);
            _deltaSubscription.Add(subscriber, context, relationType);
        }

        public void UnsubscribeLaw(Action<List<T>> subscriber) => _subscription.Remove(subscriber);
        public void UnsubscribeDelta(Action<ListDelta<T>> subscriber) => _deltaSubscription.Remove(subscriber);

        public IDisposable Bind(Action<List<T>> subscriber, object context, RxType relationType)
        {
            SubscribeLaw(subscriber, context, relationType);
            return new SubscriptionDisposable<List<T>>(this, subscriber);
        }

        public IDisposable SubscribeDeltaWithDisposable(Action<ListDelta<T>> subscriber, object context, RxType relationType)
        {
            SubscribeDelta(subscriber, context, relationType);
            return new DeltaDisposable<T>(this, subscriber);
        }
    }


    public abstract class ListDelta<T>
    {
        public sealed class Add : ListDelta<T>
        {
            public T Item { get; }
            public Add(T item) => Item = item;
        }

        public sealed class Remove : ListDelta<T>
        {
            public T Item { get; }
            public Remove(T item) => Item = item;
        }

        public sealed class Insert : ListDelta<T>
        {
            public int Index { get; }
            public T Item { get; }
            public Insert(int index, T item) { Index = index; Item = item; }
        }

        public sealed class RemoveAt : ListDelta<T>
        {
            public int Index { get; }
            public T Item { get; }
            public RemoveAt(int index, T item) { Index = index; Item = item; }
        }

        public sealed class Replace : ListDelta<T>
        {
            public int Index { get; }
            public T OldItem { get; }
            public T NewItem { get; }
            public Replace(int index, T oldItem, T newItem)
            {
                Index = index; OldItem = oldItem; NewItem = newItem;
            }
        }

        public sealed class Move : ListDelta<T>
        {
            public int OldIndex { get; }
            public int NewIndex { get; }
            public T Item { get; }
            public Move(int oldIndex, int newIndex, T item)
            {
                OldIndex = oldIndex; NewIndex = newIndex; Item = item;
            }
        }

        public sealed class Clear : ListDelta<T> { }
    }
}