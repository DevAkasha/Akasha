using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Akasha
{
    public abstract class ManagerBase : MonoBehaviour, IRxOwner, IRxCaller
    {
        public virtual int InitializationPriority => 100;

        private bool managerAwakeCalled = false;

        public bool IsRxVarOwner => true;
        public bool IsRxAllOwner => false;
        public bool IsLogicalCaller => true;
        public bool IsMultiRolesCaller => true;
        public bool IsFunctionalCaller => true;

        private readonly HashSet<RxBase> trackedRxVars = new();

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

        protected virtual void Awake()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsProjectInitialized)
            {
                return;
            }

            CallOnManagerAwake();
        }

        public void CallOnManagerAwake()
        {
            if (!managerAwakeCalled)
            {
                OnManagerAwake();
                managerAwakeCalled = true;
            }
        }

        protected virtual void OnDestroy()
        {
            OnManagerDestroy();
            Unload();
        }

        protected virtual void OnManagerAwake() { }
        public virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode) { }
        public virtual void OnSceneUnloaded(Scene scene) { }
        public virtual void OnActiveSceneChanged(Scene previousScene, Scene newScene) { }
        protected virtual void OnManagerDestroy() { }
        public virtual void OnApplicationFocusChanged(bool hasFocus) { }
        public virtual void OnApplicationPauseChanged(bool pauseStatus) { }

        protected virtual void AtEnable() { }
        protected virtual void AtAwake() { }
        protected virtual void AtStart() { }
        protected virtual void AtInit() { }
        protected virtual void AtDeinit() { }
        protected virtual void AtDisable() { }
        protected virtual void AtDestroy() { }

#if UNITY_EDITOR
        [Header("Debug Info")]
        [SerializeField, TextArea(2, 5)] protected string debugInfo;

        protected virtual void OnValidate()
        {
            if (Application.isPlaying)
            {
                debugInfo = $"Priority: {InitializationPriority}\n" +
                           $"Tracked Rx: {trackedRxVars.Count}\n" +
                           $"Type: {GetType().Name}";
            }
        }
#endif
    }
}