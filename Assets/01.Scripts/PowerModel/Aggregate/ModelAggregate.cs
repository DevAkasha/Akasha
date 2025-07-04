using System.Collections.Generic;
using UnityEngine;
namespace Akasha
{
    public abstract class ModelAggregate : AggregateRoot, IRxOwner, IRxCaller
    {
        protected virtual bool EnableSaveLoad => false;
        protected virtual bool EnablePooling => false;

        [SerializeField] protected bool isModelInitialized = false;
        public bool isDirty = false;

        public bool IsRxVarOwner => true;
        public bool IsRxAllOwner => true;
        public bool IsLogicalCaller => true;
        public bool IsMultiRolesCaller => true;
        public bool IsFunctionalCaller => true;

        private readonly HashSet<RxBase> trackedRxVars = new();

        public bool IsModelInitialized => isModelInitialized;

        public void RegisterRx(RxBase rx)
        {
            trackedRxVars.Add(rx);
        }

        public void Unload()
        {
            foreach (var rx in trackedRxVars)
            {
                rx.ClearRelation();
            }
            trackedRxVars.Clear();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (!isModelInitialized)
            {
                InitializeModel();
            }
        }

        protected override void OnDeinitialize()
        {
            if (isModelInitialized)
            {
                DeinitializeModel();
            }
            Unload();
            base.OnDeinitialize();
        }

        protected virtual void InitializeModel()
        {
            SetupModel();
            isModelInitialized = true;
            OnModelInitialized();
        }

        protected virtual void DeinitializeModel()
        {
            OnModelDeinitializing();
            CleanupModel();
            isModelInitialized = false;
        }

        protected abstract void SetupModel();
        protected virtual void CleanupModel() { }
        protected virtual void OnModelInitialized() { }
        protected virtual void OnModelDeinitializing() { }

        protected void MarkDirty()
        {
            isDirty = true;
            OnMarkDirty();
        }

        protected virtual void OnMarkDirty() { }

        public virtual void Save()
        {
            if (!EnableSaveLoad) return;

            PerformSave();
            isDirty = false;

            // 전체 게임 상태 저장
            if (GameManager.SaveLoad != null)
            {
                GameManager.SaveLoad.SaveGame();
            }
        }

        public virtual void Load()
        {
            if (!EnableSaveLoad) return;

            // 전체 게임 상태 로드
            if (GameManager.SaveLoad != null)
            {
                GameManager.SaveLoad.LoadGame();
            }

            PerformLoad();
            isDirty = false;
        }

        protected virtual void PerformSave() { }
        protected virtual void PerformLoad() { }
    }
}