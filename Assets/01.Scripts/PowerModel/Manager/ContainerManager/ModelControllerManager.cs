using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Akasha
{
    public class ModelControllerManager : ContainerManager<MController>
    {
        public override int InitializationPriority => 75;

        protected override void OnAggregateRegistered(MController controller)
        {
            Debug.Log($"[ModelControllerManager] Registered MController: {controller.GetType().Name}");
        }

        protected override void OnAggregateUnregistered(MController controller)
        {
            Debug.Log($"[ModelControllerManager] Unregistered MController: {controller.GetType().Name}");
        }

        public IEnumerable<MController> GetModelControllersByType<T>() where T : MController
        {
            return GetAll().OfType<T>();
        }

        public T GetModelController<T>() where T : MController
        {
            return GetAll().OfType<T>().FirstOrDefault();
        }

        public IEnumerable<MController> GetControllersByModelType<M>() where M : BaseModel
        {
            return GetAll().Where(c => c.GetBaseModel() is M);
        }

        public IEnumerable<MController> GetSaveLoadEnabledControllers()
        {
            return GetAll().Where(c => c.IsSaveLoadEnabled && c.IsLifecycleInitialized);
        }

        public void SaveAllModels()
        {
            var saveEnabledControllers = GetSaveLoadEnabledControllers();

            foreach (var controller in saveEnabledControllers)
            {
                if (controller.isDirty)
                {
                    controller.CallSave();
                    Debug.Log($"[ModelControllerManager] Called AtSave for {controller.GetType().Name}");
                }
            }

            Debug.Log($"[ModelControllerManager] Save process completed for {saveEnabledControllers.Count()} controllers");
        }

        public void LoadAllModels()
        {
            var saveEnabledControllers = GetSaveLoadEnabledControllers();

            foreach (var controller in saveEnabledControllers)
            {
                controller.CallLoad();
                Debug.Log($"[ModelControllerManager] Called AtLoad for {controller.GetType().Name}");
            }

            Debug.Log($"[ModelControllerManager] Load process completed for {saveEnabledControllers.Count()} controllers");
        }

        public void NotifyAllModelsReady()
        {
            var saveEnabledControllers = GetSaveLoadEnabledControllers();

            foreach (var controller in saveEnabledControllers)
            {
                controller.CallReadyModel();
                Debug.Log($"[ModelControllerManager] Called AtReadyModel for {controller.GetType().Name}");
            }

            Debug.Log($"[ModelControllerManager] All models ready - {saveEnabledControllers.Count()} controllers notified");
        }

        public void MarkAllDirty()
        {
            var saveEnabledControllers = GetSaveLoadEnabledControllers();

            foreach (var controller in saveEnabledControllers)
            {
                controller.MarkDirty();
            }

            Debug.Log($"[ModelControllerManager] Marked {saveEnabledControllers.Count()} controllers as dirty");
        }

        public int GetDirtyCount()
        {
            return GetSaveLoadEnabledControllers().Count(c => c.isDirty);
        }

        public void ClearAllDirtyFlags()
        {
            foreach (var controller in GetAll())
            {
                controller.isDirty = false;
            }
        }

        public Dictionary<string, bool> GetSaveLoadStatus()
        {
            var status = new Dictionary<string, bool>();

            foreach (var controller in GetAll())
            {
                var key = $"{controller.GetType().Name} ({controller.GetAggregateId()})";
                status[key] = controller.IsSaveLoadEnabled;
            }

            return status;
        }

        public override void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            base.OnSceneUnloaded(scene);

            var controllersInScene = GetAll()
                .Where(c => c != null && c.gameObject.scene == scene)
                .ToList();

            if (controllersInScene.Any())
            {
                Debug.Log($"[ModelControllerManager] Scene unload - processing {controllersInScene.Count} controllers");

                foreach (var controller in controllersInScene)
                {
                    if (controller.IsSaveLoadEnabled && controller.isDirty)
                    {
                        controller.CallSave();
                    }
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (Application.isPlaying)
            {
                var saveEnabledCount = GetSaveLoadEnabledControllers().Count();
                var dirtyCount = GetDirtyCount();

                debugInfo = $"Registered: {RegisteredCount}\n" +
                           $"Pooled: {PooledCount}\n" +
                           $"Save/Load Enabled: {saveEnabledCount}\n" +
                           $"Dirty Models: {dirtyCount}\n" +
                           $"Managing: {typeof(MController).Name}";
            }
        }
#endif
    }
}