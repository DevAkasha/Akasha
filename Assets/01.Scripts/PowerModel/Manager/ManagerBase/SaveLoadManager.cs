using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akasha.Modifier;
using UnityEngine;

namespace Akasha
{
    [System.Serializable]
    public class SaveData
    {
        public DateTime saveTime;
        public string gameVersion;
        public ModelControllerSaveData[] modelControllers;
    }

    [System.Serializable]
    public class ModelControllerSaveData
    {
        public ObjectMetadata objectMetadata;
        public string modelDataJson;
        public string modelTypeName;
    }

    public class SaveLoadManager : ManagerBase
    {
        public override int InitializationPriority => 60;

        [Header("Save/Load Settings")]
        [SerializeField] private string saveFileName = "gamedata.save";
        [SerializeField] private bool useEncryption = false;

        private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

        protected override void OnManagerAwake()
        {
            base.OnManagerAwake();
            Debug.Log($"[SaveLoadManager] Initialized - Save path: {SavePath}");
        }

        public bool SaveGame()
        {
            try
            {
                var saveData = CreateSaveData();
                var json = JsonUtility.ToJson(saveData, true);

                if (useEncryption)
                {
                    json = SimpleEncrypt(json);
                }

                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveLoadManager] Game saved successfully at {DateTime.Now}");

                NotifyControllersAfterSave();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoadManager] Save failed: {e.Message}");
                return false;
            }
        }

        public bool LoadGame()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning("[SaveLoadManager] Save file not found");
                return false;
            }

            try
            {
                var json = File.ReadAllText(SavePath);

                if (useEncryption)
                {
                    json = SimpleDecrypt(json);
                }

                var saveData = JsonUtility.FromJson<SaveData>(json);
                ApplySaveData(saveData);

                NotifyControllersAfterLoad();
                Debug.Log($"[SaveLoadManager] Game loaded successfully - Save time: {saveData.saveTime}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoadManager] Load failed: {e.Message}");
                return false;
            }
        }

        private SaveData CreateSaveData()
        {
            NotifyControllersBeforeSave();

            var saveData = new SaveData
            {
                saveTime = DateTime.Now,
                gameVersion = Application.version,
                modelControllers = CollectModelControllerData()
            };

            return saveData;
        }

        private ModelControllerSaveData[] CollectModelControllerData()
        {
            var modelControllers = GameManager.ModelControllers.GetAll();
            var saveDataList = new List<ModelControllerSaveData>();

            foreach (var controller in modelControllers)
            {
                if (controller == null || !controller.IsInitialized) continue;
                if (!controller.IsSaveLoadEnabled) continue;

                var model = controller.GetBaseModel();
                if (model == null) continue;

                var saveData = new ModelControllerSaveData
                {
                    objectMetadata = controller.objectMetadata,
                    modelTypeName = model.GetType().AssemblyQualifiedName,
                    modelDataJson = model.GetSaveData()
                };

                saveDataList.Add(saveData);
            }

            return saveDataList.ToArray();
        }

        private void ApplySaveData(SaveData saveData)
        {
            NotifyControllersBeforeLoad();

            foreach (var controllerData in saveData.modelControllers)
            {
                RestoreModelController(controllerData);
            }
        }

        private void RestoreModelController(ModelControllerSaveData saveData)
        {
            var metadata = saveData.objectMetadata;
            MController controller = FindExistingController(metadata);

            if (controller != null && controller.IsSaveLoadEnabled)
            {
                RestoreControllerState(controller, saveData);
            }
        }

        private MController FindExistingController(ObjectMetadata metadata)
        {
            var existingControllers = GameManager.ModelControllers.GetAll();

            foreach (var controller in existingControllers)
            {
                if (controller.objectMetadata.className == metadata.className &&
                    controller.objectMetadata.aggregateType == metadata.aggregateType)
                {
                    return controller;
                }
            }

            return null;
        }

        private void RestoreControllerState(MController controller, ModelControllerSaveData saveData)
        {
            if (!controller.IsInitialized || !controller.IsLifecycleInitialized)
            {
                Debug.LogWarning($"[SaveLoadManager] Controller {controller.GetType().Name} not ready for load");
                return;
            }

            var model = controller.GetBaseModel();
            if (model != null && !string.IsNullOrEmpty(saveData.modelDataJson))
            {
                model.LoadSaveData(saveData.modelDataJson);
            }
        }

        private void NotifyControllersBeforeSave()
        {
            var controllers = GameManager.ModelControllers.GetAll()
                .Where(c => c.IsSaveLoadEnabled && c.IsLifecycleInitialized);

            foreach (var controller in controllers)
            {
                controller.CallSave();
            }
        }

        private void NotifyControllersAfterSave()
        {
            var controllers = GameManager.ModelControllers.GetAll()
                .Where(c => c.IsSaveLoadEnabled && c.isDirty);

            foreach (var controller in controllers)
            {
                controller.isDirty = false;
            }
        }

        private void NotifyControllersBeforeLoad()
        {
            var controllers = GameManager.ModelControllers.GetAll()
                .Where(c => c.IsSaveLoadEnabled && c.IsLifecycleInitialized);

            foreach (var controller in controllers)
            {
                controller.CallLoad();
            }
        }

        private void NotifyControllersAfterLoad()
        {
            var controllers = GameManager.ModelControllers.GetAll()
                .Where(c => c.IsSaveLoadEnabled && c.IsLifecycleInitialized);

            foreach (var controller in controllers)
            {
                controller.CallReadyModel();
                controller.isDirty = false;
            }
        }

        private string SimpleEncrypt(string text)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes);
        }

        private string SimpleDecrypt(string encryptedText)
        {
            var bytes = Convert.FromBase64String(encryptedText);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public bool HasSaveFile()
        {
            return File.Exists(SavePath);
        }

        public void DeleteSaveFile()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[SaveLoadManager] Save file deleted");
            }
        }

        public SaveData GetSaveInfo()
        {
            if (!File.Exists(SavePath))
                return null;

            try
            {
                var json = File.ReadAllText(SavePath);
                if (useEncryption)
                {
                    json = SimpleDecrypt(json);
                }
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch
            {
                return null;
            }
        }

        [System.Serializable]
        private class SerializableModelData
        {
            public Dictionary<string, object> fields = new Dictionary<string, object>();
        }
    }

    public interface ISaveable
    {
        string GetSaveData();
        void LoadSaveData(string data);
    }

    public static class SaveLoadExtensions
    {
        public static void QuickSave(this GameManager gameManager)
        {
            var saveLoadManager = gameManager.GetManager<SaveLoadManager>();
            saveLoadManager?.SaveGame();
        }

        public static void QuickLoad(this GameManager gameManager)
        {
            var saveLoadManager = gameManager.GetManager<SaveLoadManager>();
            saveLoadManager?.LoadGame();
        }
    }
}