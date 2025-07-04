using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace Akasha.State
{
    public sealed class RxStateFlag : RxBase
    {
        private readonly RxVar<bool> internalFlag;

#nullable enable
        private Func<bool>? condition;
#nullable disable
        public string Name { get; }
        public bool Value => internalFlag.Value;

#nullable enable
        public event Action<bool>? OnChanged;
#nullable disable
        internal RxStateFlag(string name, IRxOwner owner)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            internalFlag = new RxVar<bool>(false, owner);
            internalFlag.AddListener(HandleChange);

            if (owner is IRxOwner model)
                model.RegisterRx(this);
        }

        private void HandleChange(bool value)
        {
            OnChanged?.Invoke(value);
        }

        internal void Set(bool value)
        {
            if (condition != null)
                throw new InvalidOperationException($"[RxStateFlag:{Name}] is condition-based.");
            internalFlag.Set(value);
        }

        internal void Evaluate()
        {
            if (condition != null)
            {
                internalFlag.Set(condition.Invoke());
            }
        }

        internal void SetCondition(Func<bool> newCondition)
        {
            condition = newCondition;
        }

        public void AddListener(Action<bool> listener)
        {
            internalFlag.AddListener(listener);
        }

        public void RemoveListener(Action<bool> listener)
        {
            internalFlag.RemoveListener(listener);
        }

        public override string ToString()
        {
            return $"[RxStateFlag] {Name} = {Value}";
        }

        public override void ClearRelation()
        {
            internalFlag.ClearRelation();
            OnChanged = null;
        }
    }

    public partial class RxStateFlagSet<TEnum> : RxBase where TEnum : Enum
    {
        private readonly List<RxStateFlag> flags;
        private readonly Dictionary<TEnum, int> indexMap;

        public RxStateFlagSet(IRxOwner owner)
        {
            if (!owner.IsRxAllOwner)
                throw new InvalidOperationException($"An invalid owner({owner}) has accessed.");

            var values = (TEnum[])Enum.GetValues(typeof(TEnum));
            flags = new List<RxStateFlag>(values.Length);
            indexMap = new Dictionary<TEnum, int>();

            for (int i = 0; i < values.Length; i++)
            {
                var enumValue = values[i];
                indexMap[enumValue] = i;
                flags.Add(new RxStateFlag(enumValue.ToString(), owner));
            }

            owner.RegisterRx(this);
        }

        public RxStateFlag this[TEnum state] => flags[indexMap[state]];

        public void SetValue(TEnum state, bool value) => this[state].Set(value);

        public bool GetValue(TEnum state) => this[state].Value;
        public void Evaluate(TEnum state) => this[state].Evaluate();

        public void EvaluateAll()
        {
            foreach (var flag in flags)
                flag.Evaluate();
        }

        public void SetCondition(TEnum state, Func<bool> condition) => this[state].SetCondition(condition);

        public void AddListener(TEnum state, Action<bool> listener) => this[state].AddListener(listener);

        public void RemoveListener(TEnum state, Action<bool> listener) => this[state].RemoveListener(listener);

        public bool AnyActive() => flags.Exists(f => f.Value);
        public bool AnyActive(params TEnum[] subset)
        {
            foreach (var flag in subset)
            {
                if (this[flag].Value)
                    return true;
            }
            return false;
        }
        public bool AllSatisfied() => flags.TrueForAll(f => f.Value);
        public bool AllSatisfied(params TEnum[] subset)
        {
            foreach (var flag in subset)
            {
                if (!this[flag].Value)
                    return false;
            }
            return true;
        }
        public bool NoneActive() => flags.TrueForAll(f => !f.Value);
        public bool NoneActive(params TEnum[] subset)
        {
            foreach (var flag in subset)
            {
                if (this[flag].Value)
                    return false;
            }
            return true;
        }
        public IEnumerable<(TEnum, bool)> Snapshot()
        {
            foreach (var pair in indexMap)
            {
                yield return (pair.Key, flags[pair.Value].Value);
            }
        }

        public IEnumerable<TEnum> ActiveFlags()
        {
            foreach (var (key, value) in Snapshot())
            {
                if (value) yield return key;
            }
        }
        public override void ClearRelation()
        {
            foreach (var flag in flags)
                flag.ClearRelation();
        }

        public override string ToString()
        {
            return $"RxStateFlagSet<{typeof(TEnum).Name}>: " + string.Join(", ", Snapshot());
        }
    }

#if UNITY_EDITOR
    public interface IRxInspectable
    {
        void DrawDebugInspector();
    }
    public partial class RxStateFlagSet<TEnum> : IRxInspectable where TEnum : Enum
    {
        public void DrawDebugInspector()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"RxStateFlagSet<{typeof(TEnum).Name}>", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (var (key, value) in Snapshot())
            {
                GUIStyle style = new(EditorStyles.label)
                {
                    normal = { textColor = value ? Color.green : Color.gray }
                };
                EditorGUILayout.LabelField(key.ToString(), value.ToString(), style);
            }
            EditorGUI.indentLevel--;
        }
    }
#endif
}