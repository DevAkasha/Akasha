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

        public void SaveAllModels()
        {
            foreach (var controller in GetAll())
            {
                if (controller is ModelAggregate modelAggregate && modelAggregate.isDirty)
                {
                    modelAggregate.Save();
                }
            }
            Debug.Log("[ModelControllerManager] Saved all dirty models");
        }

        public void LoadAllModels()
        {
            foreach (var controller in GetAll())
            {
                if (controller is ModelAggregate modelAggregate)
                {
                    modelAggregate.Load();
                }
            }
            Debug.Log("[ModelControllerManager] Loaded all models");
        }
    }
}
