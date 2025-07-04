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

                var model = controller.GetBaseModel();
                if (model == null) continue;

                var saveData = new ModelControllerSaveData
                {
                    objectMetadata = controller.objectMetadata,
                    modelTypeName = model.GetType().AssemblyQualifiedName,
                    modelDataJson = SerializeModel(model)
                };

                saveDataList.Add(saveData);
            }

            return saveDataList.ToArray();
        }

        private string SerializeModel(BaseModel model)
        {
            var modelData = new Dictionary<string, object>();
            var rxFields = model.GetAllRxFields();

            foreach (var field in rxFields)
            {
                if (field is IRxField rxField && !string.IsNullOrEmpty(rxField.FieldName))
                {
                    var value = GetFieldValue(field);
                    if (value != null)
                    {
                        modelData[rxField.FieldName] = value;
                    }
                }
            }

            return JsonUtility.ToJson(new SerializableModelData { fields = modelData });
        }

        private object GetFieldValue(RxBase field)
        {
            var fieldType = field.GetType();

            if (fieldType.IsGenericType)
            {
                var genericType = fieldType.GetGenericTypeDefinition();

                if (genericType == typeof(RxVar<>))
                {
                    var valueProperty = fieldType.GetProperty("Value");
                    return valueProperty?.GetValue(field);
                }
                else if (genericType == typeof(RxMod<>))
                {
                    var valueProperty = fieldType.GetProperty("Value");
                    return valueProperty?.GetValue(field);
                }
            }

            return null;
        }

        private void ApplySaveData(SaveData saveData)
        {
            ClearExistingControllers();

            foreach (var controllerData in saveData.modelControllers)
            {
                RestoreModelController(controllerData);
            }
        }

        private void ClearExistingControllers()
        {
            var existingControllers = GameManager.ModelControllers.GetAll().ToArray();

            foreach (var controller in existingControllers)
            {
                if (controller != null && controller.isSceneCreated)
                {
                    Destroy(controller.gameObject);
                }
            }
        }

        private void RestoreModelController(ModelControllerSaveData saveData)
        {
            var metadata = saveData.objectMetadata;
            MController controller = null;

            controller = FindExistingController(metadata);

            if (controller == null)
            {
                controller = CreateController(metadata);
            }

            if (controller != null)
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
                    controller.objectMetadata.prefabPath == metadata.prefabPath)
                {
                    if (controller.isInPool || !controller.gameObject.activeInHierarchy)
                    {
                        controller.SetInPool(false);
                        controller.gameObject.SetActive(true);
                        return controller;
                    }
                }
            }

            return null;
        }

        private MController CreateController(ObjectMetadata metadata)
        {
            GameObject prefab = null;

            if (!string.IsNullOrEmpty(metadata.prefabPath))
            {
                prefab = Resources.Load<GameObject>(metadata.prefabPath);
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Could not find prefab at path: {metadata.prefabPath}");
                return null;
            }

            var instance = Instantiate(prefab);
            var controller = instance.GetComponent<MController>();

            if (controller != null)
            {
                controller.objectMetadata = metadata;
                metadata.transformData.ApplyTo(controller.transform);

                if (metadata.hasParent && !string.IsNullOrEmpty(metadata.parentPath))
                {
                    var parent = GameObject.Find(metadata.parentPath);
                    if (parent != null)
                    {
                        controller.transform.SetParent(parent.transform);
                    }
                }
            }

            return controller;
        }

        private void RestoreControllerState(MController controller, ModelControllerSaveData saveData)
        {
            if (!controller.IsInitialized)
            {
                controller.PerformInitialization();
            }

            var model = controller.GetBaseModel();
            if (model != null && !string.IsNullOrEmpty(saveData.modelDataJson))
            {
                RestoreModelData(model, saveData.modelDataJson);
            }
        }

        private void RestoreModelData(BaseModel model, string modelDataJson)
        {
            try
            {
                var modelData = JsonUtility.FromJson<SerializableModelData>(modelDataJson);
                if (modelData?.fields == null) return;

                var rxFields = model.GetAllRxFields();

                foreach (var field in rxFields)
                {
                    if (field is IRxField rxField && modelData.fields.ContainsKey(rxField.FieldName))
                    {
                        SetFieldValue(field, modelData.fields[rxField.FieldName]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoadManager] Failed to restore model data: {e.Message}");
            }
        }

        private void SetFieldValue(RxBase field, object value)
        {
            if (value == null) return;

            var fieldType = field.GetType();

            if (fieldType.IsGenericType)
            {
                var genericType = fieldType.GetGenericTypeDefinition();
                var valueType = fieldType.GetGenericArguments()[0];

                if (genericType == typeof(RxVar<>))
                {
                    var setMethod = fieldType.GetMethod("Set");
                    var convertedValue = ConvertValue(value, valueType);
                    setMethod?.Invoke(field, new[] { convertedValue });
                }
                else if (genericType == typeof(RxMod<>))
                {
                    var setMethod = fieldType.GetMethod("Set");
                    var convertedValue = ConvertValue(value, valueType);
                    setMethod?.Invoke(field, new[] { convertedValue });
                }
            }
        }

        private object ConvertValue(object value, Type targetType)
        {
            if (targetType == typeof(int))
                return Convert.ToInt32(value);
            if (targetType == typeof(float))
                return Convert.ToSingle(value);
            if (targetType == typeof(long))
                return Convert.ToInt64(value);
            if (targetType == typeof(double))
                return Convert.ToDouble(value);
            if (targetType == typeof(bool))
                return Convert.ToBoolean(value);
            if (targetType == typeof(string))
                return Convert.ToString(value);

            return value;
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