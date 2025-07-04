using System;
using System.Collections.Generic;
using Akasha.Modifier;
using UnityEngine;

namespace Akasha
{
    public interface IModifiableTarget
    {
        IEnumerable<IModifiable> GetModifiables(); // 수정 가능한 필드 목록 반환
    }

    public abstract class BaseModel : IModifiableTarget, IRxCaller, IRxOwner, ISaveable
    {
        bool IRxCaller.IsLogicalCaller => true;
        bool IRxCaller.IsMultiRolesCaller => true;
        bool IRxCaller.IsFunctionalCaller => true;

        bool IRxOwner.IsRxVarOwner => true;
        bool IRxOwner.IsRxAllOwner => true;

        private readonly HashSet<RxBase> trackedRxVars = new();
        private readonly HashSet<IModifiable> modifiables = new();

        public void RegisterRx(RxBase rx) // Rx 필드를 모델에 등록
        {
            if (trackedRxVars.Add(rx))
            {
                if (rx is IModifiable mod)
                    RegisterModifiable(mod);
            }
        }

        protected void RegisterModifiable(IModifiable modifiable)
        {
            if (modifiable != null)
                modifiables.Add(modifiable);
        }

        public virtual IEnumerable<IModifiable> GetModifiables() => modifiables; // 수정 가능한 필드 목록 반환

        public IEnumerable<RxBase> GetAllRxFields() => trackedRxVars;

        public void Unload()
        {
            foreach (var rx in trackedRxVars)
            {
                rx.ClearRelation();
            }
            trackedRxVars.Clear();
            modifiables.Clear();
        }
        public virtual string GetSaveData()
        {
            var saveData = new Dictionary<string, object>();

            foreach (var field in trackedRxVars)
            {
                if (field is IRxField rxField && !string.IsNullOrEmpty(rxField.FieldName))
                {
                    var value = GetFieldValue(field);
                    if (value != null)
                    {
                        saveData[rxField.FieldName] = value;
                    }
                }
            }

            return JsonUtility.ToJson(saveData);
        }

        public virtual void LoadSaveData(string data)
        {
            if (string.IsNullOrEmpty(data)) return;

            try
            {
                var saveData = JsonUtility.FromJson<Dictionary<string, object>>(data);
                ApplySaveData(saveData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BaseModel] Failed to load save data: {e.Message}");
            }
        }

        private object GetFieldValue(RxBase field)
        {
            var fieldType = field.GetType();
            if (!fieldType.IsGenericType) return null;

            var valueProperty = fieldType.GetProperty("Value");
            return valueProperty?.GetValue(field);
        }

        private void ApplySaveData(Dictionary<string, object> saveData)
        {
            foreach (var field in trackedRxVars)
            {
                if (field is IRxField rxField && saveData.ContainsKey(rxField.FieldName))
                {
                    SetFieldValue(field, saveData[rxField.FieldName]);
                }
            }
        }

        private void SetFieldValue(RxBase field, object value)
        {
            var fieldType = field.GetType();
            if (!fieldType.IsGenericType) return;

            var setMethod = fieldType.GetMethod("Set");
            var valueType = fieldType.GetGenericArguments()[0];
            var convertedValue = Convert.ChangeType(value, valueType);
            setMethod?.Invoke(field, new[] { convertedValue });
        }
    }
}
