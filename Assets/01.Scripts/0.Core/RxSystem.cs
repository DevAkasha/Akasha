using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Akasha
{
    public enum RxType
    {
        Functional,
        Logical
    }

    // ----- Reactive Core -----

    public interface IRxField { }
    public interface IRxEvent
    {
        void Subscribe(Action subscriber, object context, RxType relationType);
        void Unsubscribe(Action subscriber);
        void Unsubscribe(Action subscriber, object context);
        IDisposable Bind(Action subscriber, object context);
    }

    public interface IRxReadable<T>
    {
        T Value { get; }
    }

    public interface IRxWritable<T>
    {
        void SetValue(T value, object caller);
    }

    public interface IRxSubscribable<T>
    {
        void SubscribeLaw(Action<T> subscriber, object context, RxType relationType);
        void UnsubscribeLaw(Action<T> subscriber);
    }

    public interface IRxObservable<T> : IRxReadable<T>, IRxSubscribable<T> { }

    // ----- Subscriber Role Interfaces -----

    public interface IFunctionalSubscriber { } // RxExpr, RxFlag 구독 가능
    public interface IInteractLogicalSubscriber { } // RxVar, RxList 구독 가능
    public interface IGlobalLogicalSubscriber { } // 모든 Logical 구독 가능 (Manager, Widget 등)

    // ----- Event Subscriber -----

    public interface IRxTriggerSubscriber { }
    public interface IRxLocalEventSubscriber { }
    public interface IRxGlobalEventSubscriber { }

    // ----- Ownership Marker Interfaces -----

    public interface IRxModelOwner { }
    public interface IRxFlagger { }
    public interface IScreen { }
    public interface IRxUnsafe { }

    public interface IInteractor { }
    public interface IManager { }
    public interface IPresenter { }

    // ----- Binding System -----

    public static class RxBinder
    {
        private static readonly Dictionary<object, List<IDisposable>> _bindings = new();

        public static IDisposable Bind<T>(IRxReadable<T> source, Action<T> apply, object context)
        {
            if (source is not IRxSubscribable<T> subscribable)
                throw new InvalidOperationException("[RxBind] 해당 RxReadable은 구독할 수 없습니다.");

            void Callback(T value) => apply?.Invoke(value);

            subscribable.SubscribeLaw(Callback, context, RxType.Logical);

            var disposable = new SubscriptionDisposable<T>(subscribable, Callback);

            if (!_bindings.ContainsKey(context))
                _bindings[context] = new List<IDisposable>();

            _bindings[context].Add(disposable);
            return disposable;
        }

        public static IDisposable BindEach<T>(
            RxList<T> list,
            Action<T> onAdd,
            Action<T> onRemove = null,
            object context = null
        )
        {
            void DeltaHandler(ListDelta<T> delta)
            {
                switch (delta)
                {
                    case ListDelta<T>.Add add:
                        onAdd?.Invoke(add.Item);
                        break;
                    case ListDelta<T>.Insert insert:
                        onAdd?.Invoke(insert.Item);
                        break;
                    case ListDelta<T>.Remove remove:
                        onRemove?.Invoke(remove.Item);
                        break;
                    case ListDelta<T>.RemoveAt removeAt:
                        onRemove?.Invoke(removeAt.Item);
                        break;
                    case ListDelta<T>.Replace replace:
                        onRemove?.Invoke(replace.OldItem);
                        onAdd?.Invoke(replace.NewItem);
                        break;
                    case ListDelta<T>.Clear:
                        break;
                }
            }

            list.SubscribeDelta(DeltaHandler, context, RxType.Logical);

            var disposable = new DeltaDisposable<T>(list, DeltaHandler);

            if (!_bindings.ContainsKey(context))
                _bindings[context] = new List<IDisposable>();

            _bindings[context].Add(disposable);
            return disposable;
        }

        public static void UnbindAll(object context)
        {
            if (_bindings.TryGetValue(context, out var list))
            {
                foreach (var disposable in list)
                    disposable.Dispose();

                _bindings.Remove(context);
            }
        }
    }

    public static class RxValidator
    {
        public static object? FindReactiveRoot(object obj)
        {
            if (obj is RxContextBehaviour context)
                return context.ReactiveRoot;

            return LegacyFindByTransform(obj);
        }

        private static object? LegacyFindByTransform(object obj)
        {
            var current = obj as Component;
            while (current != null)
            {
                if (current.GetComponent<BaseController>() != null)
                    return current.GetComponent<BaseController>();
                if (current.GetComponent<BasePresenter>() != null)
                    return current.GetComponent<BasePresenter>();
                current = current.transform.parent?.GetComponent<MonoBehaviour>();
            }
            return null;
        }

        public static void ValidateSubscriberContext(object context, object? fieldOwner)
        {
            string contextName = context?.GetType().Name ?? "(null)";
            string ownerName = fieldOwner?.GetType().Name ?? "(null)";

            // 이벤트(RxTrigger 등) 예외 처리: RxExpr, RxFlag는 구독 불가
            if (context is RxExpr<bool> || context is RxFlag)
            {
                throw new InvalidOperationException($"[RxValidator] {contextName}는 이벤트를 구독할 수 없습니다.");
            }

            // 인터페이스 기반 권한 체크
            bool hasAccess =
                context is IFunctionalSubscriber ||
                context is IInteractLogicalSubscriber ||
                context is IGlobalLogicalSubscriber ||
                context is IRxUnsafe;

            if (!hasAccess)
            {
                throw new InvalidOperationException(
                    $"[RxValidator] {contextName}는 Rx 객체를 구독할 권한이 없습니다.");
            }

            // UnLocal 차단: IInteractLogicalSubscriber는 같은 기준 내에서만 구독 가능
            if (context is IInteractLogicalSubscriber &&
                context is not IGlobalLogicalSubscriber &&
                context is not IRxUnsafe)
            {
                var contextRoot = FindReactiveRoot(context);
                var ownerRoot = FindReactiveRoot(fieldOwner);

                if (contextRoot != null && ownerRoot != null && contextRoot != ownerRoot)
                {
                    throw new InvalidOperationException(
                        $"[RxValidator] {contextName}는 {ownerName}의 Rx 객체를 UnLocal 구독할 수 없습니다.\n" +
                        $"↳ 기준 불일치: {contextRoot?.GetType().Name} vs {ownerRoot?.GetType().Name}");
                }
            }
        }
    }
    public static class RxEventDisposable
    {
        public static IDisposable Create(IRxEvent rxEvent, Action subscriber, object context)
        {
            rxEvent.Subscribe(subscriber, context, RxType.Logical);
            return new SubscriptionDisposable<Unit>(
                (IRxSubscribable<Unit>)rxEvent,
                _ => subscriber()
            );
        }
    }

    // ----- Execution Queue -----

    public static class RxQueue
    {
        private static readonly Queue<Action> _queue = new();
        private static readonly HashSet<object> _keys = new();
        private static readonly Dictionary<object, Action> _keyedActions = new();

        public static void Enqueue(Action action, object key = null)
        {
            if (action == null) return;

            if (key == null)
            {
                _queue.Enqueue(action);
            }
            else if (!_keys.Contains(key))
            {
                _keys.Add(key);
                _keyedActions[key] = action;
                _queue.Enqueue(() =>
                {
                    _keys.Remove(key);
                    _keyedActions.Remove(key);
                    action();
                });
            }
        }

        public static void ExecuteAll()
        {
            while (_queue.Count > 0)
            {
                var action = _queue.Dequeue();
                try { action(); }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[RxQueue] 작업 실행 중 오류: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        public static void Cancel(object key)
        {
            if (key != null && _keys.Contains(key))
            {
                _keys.Remove(key);
                _keyedActions.Remove(key);
            }
        }

        public static int Count => _queue.Count;
    }

    // ----- Reactive Context -----

    public static class RxFlow
    {
        [ThreadStatic] private static Stack<object> _contextStack;

        public static object CurrentContext => (_contextStack != null && _contextStack.Count > 0) ? _contextStack.Peek() : null;

        public static void PushContext(object context)
        {
            _contextStack ??= new Stack<object>();
            _contextStack.Push(context);
        }

        public static void PopContext() => _contextStack?.Pop();

        public static void Clear() => _contextStack?.Clear();

        public static string Snapshot()
        {
            if (_contextStack == null || _contextStack.Count == 0)
                return "[RxFlow] (empty)";
            return "[RxFlow] Stack Trace:\n" + string.Join("\n", _contextStack.Select(x => $"- {x?.GetType().Name}"));
        }
    }

    public static class RxFlowExtension
    {
        public static TResult WithContext<TResult>(this object contextOwner, Func<TResult> action)
        {
            RxFlow.PushContext(contextOwner);
            try { return action(); }
            finally { RxFlow.PopContext(); }
        }

        public static void WithContext(this object contextOwner, Action action)
        {
            RxFlow.PushContext(contextOwner);
            try { action(); }
            finally { RxFlow.PopContext(); }
        }
    }

    // ----- Disposable Helpers -----

    public class SubscriptionDisposable<T> : IDisposable
    {
        private IRxSubscribable<T> _target;
        private Action<T> _subscriber;

        public SubscriptionDisposable(IRxSubscribable<T> target, Action<T> subscriber)
        {
            _target = target;
            _subscriber = subscriber;
        }

        public void Dispose()
        {
            _target?.UnsubscribeLaw(_subscriber);
            _target = null;
            _subscriber = null;
        }
    }

    public class DeltaDisposable<T> : IDisposable
    {
        private RxList<T> _target;
        private Action<ListDelta<T>> _handler;

        public DeltaDisposable(RxList<T> target, Action<ListDelta<T>> handler)
        {
            _target = target;
            _handler = handler;
        }

        public void Dispose()
        {
            _target?.UnsubscribeDelta(_handler);
            _target = null;
            _handler = null;
        }
    }

    // ----- Subscription Logic -----
    
    internal class RxSubscription<T>
    {
        private readonly Dictionary<object, List<Subscriber>> _subscribers = new();

        private struct Subscriber
        {
            public Action<T> Callback;
            public RxType RelationType;
            public Subscriber(Action<T> callback, RxType type) { Callback = callback; RelationType = type; }
        }

        public void Add(Action<T> subscriber, object context, RxType relationType)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (!_subscribers.TryGetValue(context, out var list))
                _subscribers[context] = list = new List<Subscriber>();

            if (!list.Any(s => s.Callback == subscriber && s.RelationType == relationType))
                list.Add(new Subscriber(subscriber, relationType));
        }

        public void Remove(Action<T> subscriber)
        {
            var keysToRemove = new List<object>();
            foreach (var kvp in _subscribers)
            {
                kvp.Value.RemoveAll(sub => sub.Callback == subscriber);
                if (kvp.Value.Count == 0) keysToRemove.Add(kvp.Key);
            }
            foreach (var key in keysToRemove) _subscribers.Remove(key);
        }
        public void Remove(Action<T> subscriber, object context)
        {
            if (_subscribers.TryGetValue(context, out var list))
            {
                list.RemoveAll(sub => sub.Callback == subscriber);
                if (list.Count == 0)
                    _subscribers.Remove(context);
            }
        }

        public void NotifyAll(T value)
        {
            foreach (var list in _subscribers.Values)
                foreach (var sub in list)
                    sub.Callback?.Invoke(value);
        }

        public int SubscriberCount => _subscribers.Sum(kvp => kvp.Value.Count);
    }


    internal class RxSubscription : RxSubscription<Unit>, IRxSubscribable<Unit>
    {
        public void Add(Action subscriber, object context, RxType relationType)
            => Add(_ => subscriber(), context, relationType);

        public void Remove(Action subscriber)
            => Remove(_ => subscriber());

        public void NotifyAll()
            => NotifyAll(Unit.Default);

        // IRxSubscribable<Unit> 명시적 구현
        void IRxSubscribable<Unit>.SubscribeLaw(Action<Unit> subscriber, object context, RxType relationType)
            => Add(subscriber, context, relationType);

        void IRxSubscribable<Unit>.UnsubscribeLaw(Action<Unit> subscriber)
            => Remove(subscriber);
    }

    public readonly struct Unit { public static readonly Unit Default = new(); }
}
